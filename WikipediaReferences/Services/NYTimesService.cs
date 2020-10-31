using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WikipediaReferences.Interfaces;
using WikipediaReferences.Models;
using WikipediaReferences.Sources;

namespace WikipediaReferences.Services
{
    public class NYTimesService : INYTimesService
    {
        private readonly IWikipediaService wikipediaService;
        private readonly DateTime DateOfDeathNotFoundInObituary = DateTime.MaxValue;

        public NYTimesService(IWikipediaService wikipediaService)
        {
            this.wikipediaService = wikipediaService;
        }

        public void AddNYTimesObituaryReferences(int year, int monthId, string apiKey)
        {
            string json = GetJSONFromUrl(year, monthId, apiKey);

            NYTimesArchive archive = JsonConvert.DeserializeObject<NYTimesArchive>(json);

            var articleDocs = archive.response.docs.GroupBy(d => d._id).Select(grp => grp.First());

            // false positives: var obituaryDocs = archive.response.docs.Where(d => d.keywords.Any(k => k.value.Equals("Deaths (Obituaries)"))); 
            var obituaryDocs = articleDocs.Where(d => d.type_of_material.StartsWith("Obituary;")).ToList().OrderBy(d => d.pub_date);

            IEnumerable<Reference> references = GetReferences(monthId, year, obituaryDocs);
            references = references.OrderBy(a => a.Deathdate).ThenBy(a => a.Lastname);

            Console.WriteLine($"\r\n\r\nnr of refs: {references.Count()}!");
        }

        private IEnumerable<Reference> GetReferences(int monthId, int year, IOrderedEnumerable<Doc> obituaryDocs)
        {
            List<Reference> articles = new List<Reference>();

            foreach (Doc obituaryDoc in obituaryDocs)
            {
                string[] nameVersions = GetDeceasedNames(obituaryDoc);

                if (nameVersions == null)
                    continue;

                string articleTitle = null;

                foreach (var nameVersion in nameVersions)
                {
                    articleTitle = wikipediaService.GetArticleTitle(nameVersion, year, monthId);

                    if (articleTitle != null)
                    {
                        articles.Add(CreateReference(monthId, year, obituaryDoc, articleTitle));
                        break;
                    }
                }
            }
            return articles;
        }

        private string[] GetDeceasedNames(Doc doc)
        {
            string deceased;

            var person = doc.keywords.Where(k => k.name == "persons").FirstOrDefault();

            if (person == null)
            {
                int pos = doc.headline.main.IndexOf(',');

                if (pos < 0)
                    return null; // See feb 1997, article 17c07f7b-b7b9-5c7b-ad28-3e4649d82a08 ; A Whirl Beyond the White House for Stephanopoulos

                deceased = doc.headline.main.Substring(0, pos);
            }
            else
                deceased = person.value;

            int i = deceased.IndexOf(",");

            if (i == -1)
                return new string[] { deceased };

            string surnames = deceased.Substring(0, i);

            string firstnames = deceased.Substring(i + 1).Trim();
            firstnames = AdjustFirstNames(firstnames, out string suffix);

            return GetNameVersions(firstnames, surnames, suffix);
        }

        private string[] GetNameVersions(string firstnames, string surnames, string suffix)
        {
            if (string.IsNullOrEmpty(suffix))
            {
                if (!HasNameInitial(firstnames))
                    return new string[] { $"{firstnames} {surnames}" };
                else
                {
                    return new string[]
                    {
                        $"{FixNameInitials(firstnames, false)} {surnames}",
                        $"{FixNameInitials(firstnames, true)} {surnames}"
                    };
                }
            }
            else
            {
                if (!HasNameInitial(firstnames))
                {
                    return new string[]
                    {
                        $"{firstnames} {surnames} {suffix}",
                        $"{firstnames} {surnames}"
                    };
                }
                else
                {
                    return new string[]
                    {
                        $"{FixNameInitials(firstnames, false)} {surnames} {suffix}",
                        $"{FixNameInitials(firstnames, true)} {surnames} {suffix}",
                        $"{FixNameInitials(firstnames, false)} {surnames}",
                        $"{FixNameInitials(firstnames, true)} {surnames}"
                    };
                }
            }
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
            var names = firstnames.Split(" ");

            foreach (string name in names)
                if (IsNameInitial(name))
                    return true;

            return false;
        }

        private bool IsNameInitial(string name)
        {
            if (name.Length == 1)
                if (name.Equals(name.ToUpper()))
                    return true;

            return false;
        }

        private Reference CreateReference(int monthId, int year, Doc obituaryDoc, string articleTitle)
        {
            return new Reference()
            {
                ArticleTitle = articleTitle,
                Type = "Obituary",
                Lastname = GetLastName(articleTitle),
                //reference = CreateReference(obituaryDoc),
                Author1 = GetAuthor(obituaryDoc, false),
                Authorlink1 = GetAuthor(obituaryDoc, true),
                Title = obituaryDoc.headline.main,
                Url = obituaryDoc.web_url,
                Urlaccess = "subscription",  // https://en.wikipedia.org/wiki/Template:Citation_Style_documentation/registration
                Work = "[[The New York Times]]",
                Accessdate = DateTime.Now.Date,
                Date = obituaryDoc.pub_date.Date,
                Page = $"{obituaryDoc.print_section} {obituaryDoc.print_page}",

                Deathdate = GetDateOfDeath(obituaryDoc, monthId, year)
            };
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

        private string GetAuthor(Doc doc, bool authorlink)
        {
            string author = doc.byline.original;

            if (author == null)
                return null;
            else
            {
                if (author.Substring(0, 3).Equals("By ", StringComparison.InvariantCultureIgnoreCase))
                {
                    author = author.Substring(3);

                    string suffix = author.Substring(author.Length - 3);
                    if (suffix == " Jr" || suffix == " Sr")
                        author += ".";

                    if (authorlink)
                        return wikipediaService.GetAuthorsArticle(author, "The New York Times");
                    else
                        return author;
                }
                else
                    throw new Exception($"Invalid doc.byline.original: {author}");
            }
        }

        private DateTime GetDateOfDeath(Doc obituaryDoc, int monthId, int year)
        {
            // No date:
            // died earlier this month                              [DoD unknown]
            // Le Monde reported that she died on July 16.          [Just after excerpt]

            // Nevermind:
            // shot to death yesterday                              [see 'death' beneath here]
            // leaped to his death from a rooftop late Saturday     [see 'death' beneath here]
            // committed suicide on Nov. 5
            // committed suicide on Thursday

            // Do not use 'death' as once of the regex expressions. Not utilized in obits +:
            // https://www.nytimes.com/2018/03/27/obituaries/delores-taylor-85-dies-writer-and-star-in-billy-jack-films.html
            // - do not look in doc.snippet: is identical to doc.abstract or is empty.

            DateTime dateOfDeath = DateTime.MinValue;

            foreach (var excerpt in new string[] { obituaryDoc.lead_paragraph, obituaryDoc.@abstract })
            {
                string monthName = GetMonthOfDeath(excerpt, monthId, out string search);

                if (monthName != null)
                {
                    dateOfDeath = GetDateOfDeathFromMonth(excerpt, monthName, year, search);

                    if (dateOfDeath != DateTime.MinValue)
                        return dateOfDeath;
                }
            }

            foreach (var excerpt in new string[] { obituaryDoc.lead_paragraph, obituaryDoc.@abstract })
            {
                string dayName = GetDayOfDeath(excerpt);

                if (dayName != null)
                    return GetDateOfDeathFromDay(obituaryDoc.pub_date.Date, dayName); // .Date: loose hours
            }

            dateOfDeath = GetDateOfDeathFromDayExpressions(obituaryDoc);

            if (dateOfDeath != DateTime.MinValue)
                return dateOfDeath;

            // Not found
            return DateOfDeathNotFoundInObituary;
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
                            throw new ArgumentException($"Expression not implemented: {dayExpression}");
                    }
                }
            }
            return DateTime.MinValue;
        }

        private DateTime GetDateOfDeathFromMonth(string excerpt, string monthName, int year, string search)
        {
            int pos = excerpt.IndexOf(search);
            string dayString = GetValueInBetweenSeparators(excerpt, " ", pos + search.Length);

            // f.i.: "died in early July when he lost his way in blizzard" [Error: parse 'when']
            if (!int.TryParse(dayString, out int day))
                return DateTime.MinValue;

            return DateTime.Parse($"{day} {monthName} {year}"); // I removed FixMonthName(monthName)
        }

        private string GetValueInBetweenSeparators(string text, string separator, int startIndex)
        {
            int pos1 = text.IndexOf(separator, startIndex);

            if (pos1 == -1)
                throw new Exception($"pos1; value not found: '{separator}'");

            int pos2 = text.IndexOf(separator, pos1 + 1);

            if (pos2 == -1)
                throw new Exception($"pos2; value not found: '{separator}'");

            string value = text.Substring(pos1 + 1, pos2 - pos1 - 1);

            return value.Replace(".", string.Empty);
        }

        private DateTime GetDateOfDeathFromDay(DateTime publicationDate, string dayName)
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
                    throw new Exception($"Day name not found: {dayName}");
            }

            return dateOfDeath;
        }

        private string GetDayName(DateTime date, CultureInfo cultureInfo)
        {
            return cultureInfo.DateTimeFormat.GetDayName(date.DayOfWeek);
        }

        private string GetMonthOfDeath(string excerpt, int monthId, out string search)
        {
            search = null;

            if (excerpt == null)
                return null;

            var monthArray = GetMonthsArray(monthId);

            // month name (abbrevation) will be followed by the day (f.i.: died on Oct. 4)
            for (int i = 0; i < 3; i++)
            {
                // died Aug. 8
                // died at his home in Fort Lauderdale, Fla., on Oct. 23
                // was found dead on July 9 near Llangollen            
                Regex rgMonth = new Regex(" (?:died|dead|killed) .{0,60}" + monthArray.ElementAt(i));

                var matches = rgMonth.Match(excerpt);

                if (matches.Success)
                {
                    search = matches.Value;
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
                    return new List<string> { monthNames.ElementAt(0), monthNames.ElementAt(11), monthNames.ElementAt(10) };
                case 2:
                    return new List<string> { monthNames.ElementAt(1), monthNames.ElementAt(0), monthNames.ElementAt(11) };
                default:
                    return new List<string> { monthNames.ElementAt(monthId), monthNames.ElementAt(monthId - 1), monthNames.ElementAt(monthId - 2) };
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

        private string GetDayOfDeath(string excerpt)
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

            string uri = @"https://api.nytimes.com/svc/archive/v1/" +  @$"{year}/{monthId}.json?api-key={apiKey}";

            Console.WriteLine("Retrieving JSON from the NYTime Archive. Please wait...\r\n");
            // by calling .Result you are synchronously reading the result
            var response = client.GetAsync(uri).Result;

            if (response.IsSuccessStatusCode)
                return response.Content.ReadAsStringAsync().Result;

            return null;
        }
    }
}
