﻿using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.RegularExpressions;
using Wikimedia.Utilities.Exceptions;
using WikipediaReferences.Data;
using WikipediaReferences.Interfaces;
using WikipediaReferences.Models;
using WikipediaReferences.Sources;

namespace WikipediaReferences.Services
{
    public class NYTimesService : INYTimesService
    {
        private readonly WRContext context;
        private readonly IWikipediaService wikipediaService;
        private readonly DateTime DateOfDeathNotFoundInObituary = DateTime.MaxValue;

        public NYTimesService(WRContext context, IWikipediaService wikipediaService)
        {
            this.context = context;
            this.wikipediaService = wikipediaService;
        }

        public IEnumerable<Reference> GetReferencesPerDeathDate(DateTime deathDate)
        {
            return context.References.Where(r => r.DeathDate == deathDate);
        }

        public IEnumerable<Reference> GetReferencesPerMonthOfDeath(int year, int monthId)
        {
            return context.References.Where(r => r.DeathDate.Year == year && r.DeathDate.Month == monthId);
        }

        public IEnumerable<Reference> GetReferencesByArticleTitle(string articleTitle)
        {
            return context.References.Where(r => r.SourceCode == "NYT" && r.ArticleTitle.StartsWith(articleTitle));
        }

        public Reference UpdateDeathDate(IEnumerable<Reference> references, Dtos.UpdateDeathDate updateDeathDateDto)
        {
            // mini mapper
            references.ToList().ForEach(r => r.DeathDate = updateDeathDateDto.DeathDate);

            context.UpdateRange(references);

            context.SaveChanges();

            return references.First();
        }

        public string AddObituaryReferences(int year, int monthId, string apiKey)
        {
            IEnumerable<Reference> references = GetReferencesPerArchiveMonth(year, monthId);

            if (references.Any())
                throw new ArgumentException($"\r\nNYT archive month has already been added; {references.Count()} refs found. " +
                                    $"Month: {GetMonthNames(false).ElementAt(monthId - 1)} {year}");

            string json = GetJSONFromUrl(year, monthId, apiKey);
            IEnumerable<Doc> articleDocs = GetArticleDocs(json);
            IEnumerable<Doc> obituaryDocs = GetObituaryDocs(year, monthId, articleDocs);
            references = GetReferencesFromArchive(monthId, year, obituaryDocs);
            references = references.OrderBy(a => a.DeathDate).ThenBy(a => a.LastNameSubject);

            context.References.AddRange(references);

            context.SaveChanges();

            string message = $"{references.Count()} NYTimes obituary references have been saved succesfully.";
            Console.WriteLine(message);

            return message;
        }

        private IEnumerable<Reference> GetReferencesPerArchiveMonth(int year, int monthId)
        {
            DateTime archiveDate = GetArchiveDate(year, monthId);
            return context.References.Where(r => r.ArchiveDate == archiveDate);
        }

        private IEnumerable<Doc> GetArticleDocs(string json)
        {
            NYTimesArchive archive = JsonConvert.DeserializeObject<NYTimesArchive>(json);
            var articleDocs = archive.response.docs.GroupBy(d => d._id).Select(grp => grp.First());
            // false positives: var obituaryDocs = archive.response.docs.Where(d => d.keywords.Any(k => k.value.Equals("Deaths (Obituaries)")))
            return articleDocs;
        }

        private IEnumerable<Doc> GetObituaryDocs(int year, int monthId, IEnumerable<Doc> articleDocs)
        {
            try
            {
                return articleDocs.Where(d => d.type_of_material.Contains("Obituary")).AsEnumerable().OrderBy(d => d.pub_date);
            }
            catch (Exception) // Not every articleDoc has a property type_of_material
            {
                return GetObituaryDocsNoLinq(year, monthId, articleDocs);
            }
        }

        private List<Doc> GetObituaryDocsNoLinq(int year, int monthId, IEnumerable<Doc> articleDocs)
        {
            List<Doc> obituaryDocs = new List<Doc>();

            foreach (var doc in articleDocs)
            {
                try
                {
                    if (doc.type_of_material.Contains("Obituary"))
                        obituaryDocs.Add(doc);
                }
                catch (Exception)
                {
                    Console.WriteLine($"Doc object has no property type_of_material. Year: {year} Month: {monthId} doc Id: {doc._id}");
                }
            }
            _ = obituaryDocs.OrderBy(d => d.pub_date);

            return obituaryDocs;
        }

        private IEnumerable<Reference> GetReferencesFromArchive(int monthId, int year, IEnumerable<Doc> obituaryDocs)
        {
            List<Reference> references = new List<Reference>();

            foreach (Doc obituaryDoc in obituaryDocs)
            {
                string[] nameVersions = ResolveNameVersions(obituaryDoc);

                if (nameVersions == null)
                    continue;

                string articleTitle = null;

                foreach (var nameVersion in nameVersions)
                {
                    articleTitle = CheckIfNameVersionExistsAsArticle(monthId, year, nameVersion);

                    if (articleTitle != null)
                    {
                        references.Add(CreateReference(monthId, year, obituaryDoc, articleTitle));
                        break;
                    }
                }
            }
            return references;
        }

        private string CheckIfNameVersionExistsAsArticle(int monthId, int year, string nameVersion)
        {
            string articleTitle = null;
            try
            {
                articleTitle = wikipediaService.GetArticleTitle(nameVersion, year, monthId);
            }
            catch (WikipediaPageNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            return articleTitle;
        }

        private string[] ResolveNameVersions(Doc doc)
        {
            string name = ResolveNameValue(doc);

            if (name == null)
                return new string[0];

            int i = name.IndexOf(",");

            if (i == -1) // Just one name
                return new string[] { Capitalize(name) };

            // "BAUMFELD," in request March 1988
            if (!name.Contains(" "))
            {
                return new string[] { Capitalize(name.Replace(",", ""))};
            }


            string surnames = Capitalize(name.Substring(0, i));

            string firstnames = Capitalize(name.Substring(i + 1).Trim());
            firstnames = AdjustFirstNames(firstnames, out string suffix);

            return GetNameVersions(firstnames, surnames, suffix);
        }

        private string ResolveNameValue(Doc doc)
        {
            var person = doc.keywords.FirstOrDefault(k => k.name == "persons");

            if (person != null)
                return person.value;
            else
            {
                int pos = doc.headline.main.IndexOf(',');

                if (pos < 0)
                    return null; // See feb 1997, article 17c07f7b-b7b9-5c7b-ad28-3e4649d82a08 ; A Whirl Beyond the White House for Stephanopoulos

                return doc.headline.main.Substring(0, pos);
            }
        }

        private string Capitalize(string value)
        {
            value = value.Replace("  ", " ");

            string[] values = value.Split(" ");
            string capitalized = String.Empty;

            values.ToList().ForEach(v => capitalized += Char.ToUpper(v.First()) + v.Substring(1).ToLower() + " ");

            return capitalized.Trim();
        }

        private string[] GetNameVersions(string firstnames, string surnames, string suffix)
        {
            if (string.IsNullOrEmpty(suffix))
            {
                if (HasNameInitial(firstnames))
                    return GetNameVersionsInitialsNoSuffix(firstnames, surnames);
                else
                    return new string[] { $"{firstnames} {surnames}" };
            }
            else
            {
                if (HasNameInitial(firstnames))
                    return GetNameVersionsInitials(firstnames, surnames, suffix);
                else
                    return GetNameVersionsNoInitials(firstnames, surnames, suffix);
            }
        }

        private string[] GetNameVersionsNoInitials(string firstnames, string surnames, string suffix)
        {
            return new string[]
            {
                $"{firstnames} {surnames} {suffix}",
                $"{firstnames} {surnames}"
            };
        }

        private string[] GetNameVersionsInitials(string firstnames, string surnames, string suffix)
        {
            return new string[]
            {
                $"{FixNameInitials(firstnames, false)} {surnames} {suffix}",
                $"{FixNameInitials(firstnames, true)} {surnames} {suffix}",
                $"{FixNameInitials(firstnames, false)} {surnames}",
                $"{FixNameInitials(firstnames, true)} {surnames}"
            };
        }

        private string[] GetNameVersionsInitialsNoSuffix(string firstnames, string surnames)
        {
            return new string[]{
                $"{FixNameInitials(firstnames, false)} {surnames}",
                $"{FixNameInitials(firstnames, true)} {surnames}" };
        }

        private string AdjustFirstNames(string firstnames, out string suffix)
        {
            suffix = null;

            if (firstnames.Length < 3)
                return firstnames;

            string tail = firstnames.Substring(firstnames.Length - 3);

            // Loose any Jr or Sr (which is part of first names because of the comma separator)
            switch (tail)
            {
                case " Jr":
                case " Sr":
                    suffix = tail.Substring(1) + ".";
                    return firstnames[0..^3];
                default:
                    return firstnames;
            }
        }

        private string FixNameInitials(string firstnames, bool remove)
        {
            string @fixed = "";

            var names = firstnames.Split(" ");

            foreach (string name in names)
            {
                if (IsNameInitial(name))
                {
                    if (!remove)
                        @fixed += $"{name}. ";
                }
                else
                    @fixed += $"{name} ";
            }
            return @fixed.Trim();
        }

        private bool HasNameInitial(string firstnames)
        {
            foreach (string name in firstnames.Split(" "))
                if (IsNameInitial(name))
                    return true;

            return false;
        }

        private bool IsNameInitial(string name)
        {
            if (name.Length == 1 && name.Equals(name.ToUpper()))
                return true;

            return false;
        }

        private Reference CreateReference(int monthId, int year, Doc obituaryDoc, string articleTitle)
        {
            string author = obituaryDoc.byline.original;
            string agency = null;

            DetermineReferenceParameters(ref author, ref agency);

            return new Reference()
            {
                ArticleTitle = articleTitle,
                Type = "Obituary",
                SourceCode = "NYT",
                LastNameSubject = GetLastName(articleTitle),
                Author1 = GetAuthor(author, false),
                Authorlink1 = GetAuthor(author, true),
                Title = obituaryDoc.headline.main,
                Url = obituaryDoc.web_url,
                UrlAccess = "subscription",  // https://en.wikipedia.org/wiki/Template:Citation_Style_documentation/registration
                Quote = obituaryDoc.lead_paragraph,
                Work = "The New York Times",
                Agency = agency,
                Publisher = null,
                Language = "en-us",
                Location = "New York City",
                AccessDate = DateTime.Now.Date,
                Date = obituaryDoc.pub_date.Date,
                Page = $"{obituaryDoc.print_section} {obituaryDoc.print_page}",
                DeathDate = ResolveDateOfDeath(obituaryDoc, monthId, year),
                ArchiveDate = GetArchiveDate(year, monthId)
            };
        }

        private void DetermineReferenceParameters(ref string author, ref string agency)
        {
            switch (author)
            {
                case "AP":
                    author = null;
                    agency = "The Associated Press";
                    break;
                case "Reuters":
                case "New York Times Regional Newspapers":
                    author = null;
                    agency = author;
                    break;
            }
        }

        private DateTime GetArchiveDate(int year, int monthId)
        {
            return new DateTime(year, monthId, 1);
        }

        private string GetLastName(string articleTitle)
        {
            string[] parts = articleTitle.Split(" ");

            int i;
            for (i = parts.Length - 1; i > 0; i--)
            {
                if (parts[i].StartsWith("("))
                    break;
            }

            if (i == 0)
                return parts[^1];
            else
                return parts[i - 1];
        }

        private string GetAuthor(string author, bool authorlink)
        {
            if (author == null)
                return null;
            else
            {
                if (author.Length < 3)
                    return author;

                if (author.Substring(0, 3).Equals("By ", StringComparison.InvariantCultureIgnoreCase))
                {
                    author = author.Substring(3);

                    if (author.Length < 3)
                        return author;

                    string suffix = author.Substring(author.Length - 3);
                    if (suffix == " Jr" || suffix == " Sr")
                        author += ".";

                    if (authorlink)
                        return wikipediaService.GetAuthorsArticle(author, "The New York Times");
                    else
                        return author;
                }
                else
                {
                    // bugfix: https://timesmachine.nytimes.com/timesmachine/1994/05/16/108898.html?pageNumber=26
                    Console.WriteLine($"Invalid doc.byline.original: {author}");
                    return null;
                }
            }
        }

        public DateTime ResolveDateOfDeath(Doc obituaryDoc, int monthId, int year)
        {
            // No date:
            // died earlier this month                              [DoD unknown]
            // Le Monde reported that she died on July 16.          [Just after excerpt]

            // Nevermind:
            // shot to death yesterday                              [see 'death' beneath here]
            // leaped to his death from a rooftop late Saturday     [see 'death' beneath here]
            // committed suicide on Thursday

            // Do not use 'death' as one of the regex expressions. Not utilized in obits +:
            // https://www.nytimes.com/2018/03/27/obituaries/delores-taylor-85-dies-writer-and-star-in-billy-jack-films.html
            // - do not look in doc.snippet: is identical to doc.abstract or is empty.

            DateTime dateOfDeath = DateTime.MinValue;

            dateOfDeath = GetDateOfDeathFromMonthInformation(obituaryDoc, monthId, year, dateOfDeath);

            if (dateOfDeath != DateTime.MinValue)
                return dateOfDeath;

            string dayName = GetDayNameOfDeath(obituaryDoc);

            if (dayName != null)
                return GetDateOfDeathFromDayName(obituaryDoc.pub_date.Date, dayName); // .Date: loose hours

            dateOfDeath = GetDateOfDeathFromDayExpressions(obituaryDoc);

            if (dateOfDeath != DateTime.MinValue)
                return dateOfDeath;

            // Not found
            return DateOfDeathNotFoundInObituary;
        }

        private string GetDayNameOfDeath(Doc obituaryDoc)
        {
            foreach (var excerpt in new string[] { obituaryDoc.lead_paragraph, obituaryDoc.@abstract })
            {
                string dayName = GetDayNameOfDeathFromExcerpt(excerpt);

                if (dayName != null)
                    return dayName;
            }
            return null;
        }

        private DateTime GetDateOfDeathFromMonthInformation(Doc obituaryDoc, int monthId, int year, DateTime dateOfDeath)
        {
            foreach (var excerpt in new string[] { obituaryDoc.lead_paragraph, obituaryDoc.@abstract })
            {
                string monthName = GetMonthOfDeath(excerpt, monthId, out string matchedValue);

                if (monthName != null)
                {
                    dateOfDeath = GetDateOfDeathFromMonth(excerpt, monthName, year, matchedValue, obituaryDoc.pub_date.Date);

                    if (dateOfDeath != DateTime.MinValue)
                        return dateOfDeath;
                }
            }

            return dateOfDeath;
        }

        private DateTime GetDateOfDeathFromDayExpressions(Doc obituaryDoc)
        {
            // Create regex for handling people who died yesterday, today, this morning etc.:
            // died yesterday, died early yesterday, died in his sleep yesterday
            // *** BTW never present: "died the day before yesterday" ***
            // died today, died at a clinic here today
            // was killed in a car crash early this morning                                    
            // died here this afternoon at home
            string[] dayExpressions = new string[] { "yesterday", "today", "this morning", "this afternoon", "this evening" };

            foreach (string dayExpression in dayExpressions)
            {
                DateTime dateOfDeath = GetDateOfDeathFromDayExpression(obituaryDoc, dayExpression);

                if (dateOfDeath != DateTime.MinValue)
                    return dateOfDeath;
            }

            return DateTime.MinValue;
        }

        private DateTime GetDateOfDeathFromDayExpression(Doc obituaryDoc, string dayExpression)
        {
            Regex regex = new Regex(" (?:died|dead|killed) .{0,60}" + dayExpression); // start/end wildcard not needed (= .*)

            foreach (var excerpt in new string[] { obituaryDoc.lead_paragraph, obituaryDoc.@abstract })
            {
                if (excerpt == null)
                    break;

                var matches = regex.Match(excerpt);

                if (matches.Success)
                {
                    switch (dayExpression)
                    {
                        case "yesterday":
                            return obituaryDoc.pub_date.AddDays(-1).Date;
                        case "today":
                        case "this morning":
                        case "this afternoon":
                        case "this evening":
                            return obituaryDoc.pub_date.Date;
                        default:
                            throw new ArgumentException($"\r\nExpression not implemented: {dayExpression}");
                    }
                }
            }
            return DateTime.MinValue;
        }

        private DateTime GetDateOfDeathFromMonth(string excerpt, string monthName, int year, string matchedValue, DateTime publicationDate)
        {
            int pos = excerpt.IndexOf(matchedValue);
            string dayString = GetValueInBetweenSeparators(excerpt, " ", pos + matchedValue.Length);

            // bugfix: https://www.nytimes.com/1990/12/18/obituaries/a-gardiner-creel-80-island-s-co-owner-dies.html : 'died yesterday' AND 'died in July.' (the husband)
            if (dayString == null)
                return DateTime.MinValue;

            // f.i.: "died in early July when he lost his way in blizzard" [Error: parse 'when']
            if (!int.TryParse(dayString, out int day))
                return DateTime.MinValue;

            // https://www.nytimes.com/1997/01/18/arts/mae-barnes-89-jazz-singer-famous-for-the-charleston.html
            // https://www.nytimes.com/2003/01/09/arts/richard-mohr-83-impresario-of-radio-opera-intermissions.html
            // https://www.nytimes.com/1995/02/11/obituaries/dr-eli-robins-73-challenger-of-freudian-psychiatry-is-dead.html
            switch (monthName)
            {
                case "Oct":
                case "Nov":
                case "Dec":
                    if (publicationDate.Month <= 3)
                        year--;
                    break;
            }

            return DateTime.Parse($"{day} {monthName} {year}");
        }

        private string GetValueInBetweenSeparators(string text, string separator, int startIndex)
        {
            int pos1 = text.IndexOf(separator, startIndex);

            if (pos1 == -1)
                return null;

            int pos2 = text.IndexOf(separator, pos1 + 1);

            if (pos2 == -1)
                // We're at the end.
                pos2 = text.Length;

            string value = text.Substring(pos1 + 1, pos2 - pos1 - 1);

            return value.Replace(".", string.Empty);
        }

        private DateTime GetDateOfDeathFromDayName(DateTime publicationDate, string dayName)
        {
            DateTime dateOfDeath = publicationDate.AddDays(-1); // [[Jan Šejna]]: 'died...on Saturday' means 'last Saterday' (like [[John Kendrew]]).
            int i = 0;
            CultureInfo ci = new CultureInfo("en-US");

            while (GetDayName(dateOfDeath, ci) != dayName)
            {
                dateOfDeath = dateOfDeath.AddDays(-1);

                // Sanity check to prevent endless loop.
                i++;
                if (i > 7)
                    throw new ArgumentException($"\r\nDay name not found: {dayName}");
            }

            return dateOfDeath;
        }

        private string GetDayName(DateTime date, CultureInfo cultureInfo)
        {
            return cultureInfo.DateTimeFormat.GetDayName(date.DayOfWeek);
        }

        private string GetMonthOfDeath(string excerpt, int monthId, out string matchedValue)
        {
            matchedValue = null;

            if (excerpt == null)
                return null;

            var monthArray = GetMonthsArray(monthId);

            // month name (abbrevation) will be followed by the day (f.i.: died on Oct. 4)
            for (int i = 0; i < monthArray.Count; i++)
            {
                // died Aug. 8
                // died at his home in Fort Lauderdale, Fla., on Oct. 23
                // was found dead on July 9 near Llangollen            
                Regex rgMonth = new Regex(" (?:died|dead|killed) .{0,60}" + monthArray.ElementAt(i));

                var matches = rgMonth.Match(excerpt);

                if (matches.Success)
                {
                    matchedValue = matches.Value;
                    return monthArray.ElementAt(i);
                }
            }
            return null;
        }

        private List<string> GetMonthsArray(int monthId)
        {
            var monthNames = GetMonthNames(true);

            switch (monthId)
            {
                case 1:
                    return new List<string> { monthNames.ElementAt(0), monthNames.ElementAt(11), monthNames.ElementAt(10), monthNames.ElementAt(9), monthNames.ElementAt(8), monthNames.ElementAt(7) };
                case 2:
                    return new List<string> { monthNames.ElementAt(1), monthNames.ElementAt(0), monthNames.ElementAt(11), monthNames.ElementAt(10), monthNames.ElementAt(9), monthNames.ElementAt(8) };
                case 3:
                    return new List<string> { monthNames.ElementAt(2), monthNames.ElementAt(1), monthNames.ElementAt(0), monthNames.ElementAt(11), monthNames.ElementAt(10), monthNames.ElementAt(9) };
                case 4:
                    return new List<string> { monthNames.ElementAt(3), monthNames.ElementAt(2), monthNames.ElementAt(1), monthNames.ElementAt(0), monthNames.ElementAt(11), monthNames.ElementAt(10) };
                case 5:
                    return new List<string> { monthNames.ElementAt(4), monthNames.ElementAt(3), monthNames.ElementAt(2), monthNames.ElementAt(1), monthNames.ElementAt(0), monthNames.ElementAt(11) };
                default:
                    return new List<string> { monthNames.ElementAt(monthId - 1), monthNames.ElementAt(monthId - 2), monthNames.ElementAt(monthId - 3),
                                              monthNames.ElementAt(monthId - 4), monthNames.ElementAt(monthId - 5), monthNames.ElementAt(monthId - 6) };
            }
        }

        private List<string> GetMonthNames(bool abbreviated)
        {
            List<string> monthNames;

            if (abbreviated)
                return new List<string>() { "Jan", "Feb", "March", "April", "May", "June",
                                            "July", "Aug", "Sep", "Oct", "Nov", "Dec" };
            else
            {
                monthNames = CultureInfo.GetCultureInfo("en-US").DateTimeFormat.MonthNames.ToList();
                //Trunc 13th month
                monthNames.RemoveAt(monthNames.Count - 1);
            }

            return monthNames;
        }

        private string GetDayNameOfDeathFromExcerpt(string excerpt)
        {
            if (excerpt == null)
                return null;

            var dayNames = CultureInfo.GetCultureInfo("en-US").DateTimeFormat.DayNames.ToList();

            // died of undisclosed causes in a clinic outside Paris on Friday
            // was declared dead on arrival on Saturday
            // was killed in a traffic accident in Japan on Saturday
            foreach (var dayName in dayNames)
            {
                Regex rgDay = new Regex(" (?:died|dead|killed) .{0,60}" + dayName); // start/end wildcard not needed (= .*)

                var matches = rgDay.Match(excerpt);

                if (matches.Success)
                    return dayName;
            }
            return null;
        }

        private string GetJSONFromUrl(int year, int monthId, string apiKey)
        {
            using var client = new HttpClient();

            var assemblyName = Assembly.GetExecutingAssembly().GetName();
            string agent = $"{assemblyName.Name} v.{assemblyName.Version}";

            client.DefaultRequestHeaders.Add("User-Agent", agent);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            string uri = @"https://api.nytimes.com/svc/archive/v1/" + @$"{year}/{monthId}.json?api-key={apiKey}";

            Console.WriteLine("####### JSON is being retrieved from the NYTimes archive. #######");
            // by calling .Result you are synchronously reading the result
            var response = client.GetAsync(uri).Result;

            if (response.IsSuccessStatusCode)
                return response.Content.ReadAsStringAsync().Result;

            return null;
        }
    }
}
