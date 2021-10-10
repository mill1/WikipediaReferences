using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using WikipediaReferences.Interfaces;

namespace WikipediaReferences.Services
{
    public class WikipediaService : IWikipediaService
    {
        private const string UrlWikipediaRawBase = "https://en.wikipedia.org/w/index.php?action=raw&title=";
        private const string EntryDelimiter = "*[[";        
        private const int NoInfobox = -1;

        private readonly ILogger logger;

        public WikipediaService(ILogger<WikipediaService> logger)
        {
            this.logger = logger;
        }

        // Why bother?
        private DateTime CheckIfIsException(string linkedName, out bool isException)
        {
            isException = true;

            if (linkedName == "Josef Gingold")
                return DateTime.Parse("October 28 1909"); // OldStyleDate
            else if (linkedName == "Mehr Abdul Haq")
                return DateTime.Parse("1 June 1915"); // {{birth date|1915|6|1|df=yes}}
            else if (linkedName == "XXX")
                return DateTime.Parse("some_date");
            else if (linkedName == "XXX")
                return DateTime.Parse("some_date");
            else if (linkedName == "XXX")
                return DateTime.Parse("some_date");
            else if (linkedName == "XXX")
                return DateTime.Parse("some_date");
            else if (linkedName == "XXX")
                return DateTime.Parse("some_date");
            else if (linkedName == "XXX")
                return DateTime.Parse("some_date");
            else if (linkedName == "XXX")
                return DateTime.Parse("some_date");
            else if (linkedName == "XXX")
                return DateTime.Parse("some_date");
            else if (linkedName == "XXX")
                return DateTime.Parse("some_date");

            isException = false;
            return DateTime.Now;
        }

        
        private Entry ParseEntry(string rawEntry, DateTime deathDate)
        {
            var linkedName = GetNameFromRawEntry(rawEntry, true, out string articleText);
            var name = GetNameFromRawEntry(rawEntry, false, out string dummy);

            var information = GetInformationFromRawEntry(rawEntry);
            information = TransformInformation(information, articleText, linkedName, deathDate);

            return new Entry
            {
                LinkedName = linkedName,
                Name = name,
                Information = information,
                Reference = GetReferencesFromRawEntry(rawEntry),
                DeathDate = deathDate
            };
        }

        private string TransformInformation(string information, string articleText, string linkedName, DateTime dateOfDeath)
        {
            var dateOfBirth = ResolveDateOfBirth(articleText, linkedName, out bool dateOfBirthUnknown, out bool yearOfBirthOnly);

            if (dateOfBirthUnknown)
                return RemoveBornPartFromInformation(information, linkedName);

            string age = GetAgeAsString(dateOfBirth, dateOfDeath, yearOfBirthOnly);
            information = RemoveBornPartFromInformation(information, linkedName);

            if (!information.EndsWith("."))
                information += ".";

            return age + ", " + information;
        }

        private DateTime ResolveDateOfBirth(string articleText, string linkedName, out bool dateOfBirthUnknown,  out bool yearOfBirthOnly)
        {
            yearOfBirthOnly = false;
            dateOfBirthUnknown = false;
            DateTime dateOfBirth = CheckIfIsException(linkedName, out bool isException);

            if (isException)
                return dateOfBirth;

            articleText = articleText.Replace("&nbsp;", " ");
            articleText = articleText.Replace("born ", "");
            articleText = articleText.Replace("b. ", "");
            articleText = RemoveRefInfo(articleText);

            int pos1 = GetStartPositionDoB(articleText, linkedName, out string startOpeningSentence);
            int pos2 = ResolvePositionYearOfBirth(articleText, pos1 + startOpeningSentence.Length) + 4;

            startOpeningSentence = ResolveStartOpeningSentence(articleText, pos1, pos2, startOpeningSentence);

            string birthdateString = articleText.Substring(pos1 + startOpeningSentence.Length, pos2 - (pos1 + startOpeningSentence.Length) - 1);

            Console.WriteLine(birthdateString); // ######q1#########################################

            birthdateString = birthdateString.Trim();

            // Died?
            if(birthdateString.Contains("died ") || birthdateString.Contains("d. ") || birthdateString.Contains("circa ") || birthdateString.Contains("c. ") || birthdateString.Contains("ca "))
            {
                dateOfBirthUnknown = true;
                return DateTime.MinValue;
            }

            if (birthdateString.Length == 4 && IsNumeric(birthdateString))
            {
                yearOfBirthOnly = true;
                return new DateTime(int.Parse(birthdateString), 1, 1);
            }

            if (birthdateString.Length > 18)
                throw new Exception($"Article {linkedName}: birthdateString is too long: '{birthdateString}'");

            try
            {
                dateOfBirth = DateTime.Parse(birthdateString);
            }
            catch (Exception)
            {
                throw new Exception($"Article {linkedName}: Date could not be parsed. birthdateString: '{birthdateString}'");
            }

            return dateOfBirth;
        }

        // Get the text preceding the day of birth in the opening sentence.
        private int GetStartPositionDoB(string articleText, string linkedName, out string startOpeningSentence)
        {
            // Find first '''
            var pos0 = articleText.IndexOf("'''");
            var pos1 = articleText.IndexOf("'''", pos0 + 1);

            if (pos1 == -1)
                throw new Exception($"Article {linkedName}: Corresponding ''' not found in opening sentence!");

            // yeah yeah
            if (pos1 >= 21)
                if (articleText.Substring(pos1 - 21, 24) == "DavisCupresult = '''W'''")
                {
                    pos0 = articleText.IndexOf("'''", pos1 + 1);
                    pos1 = articleText.IndexOf("'''", pos0 + 1);
                }                    

            if (pos1 == -1)
                throw new Exception($"Not found: ['''*], Article: {linkedName}");


            startOpeningSentence = articleText.Substring(pos0, pos1 - (pos0 - "'''".Length));

            return pos0; // Is pos1 in calling method
        }

        private string ResolveStartOpeningSentence(string articleText, int pos1, int pos2, string startOpeningSentence)
        {
            // Re-evaluate start position DoB by looking back from encountered year of birth; look for specific preceding chars.
            int posSemiColon = articleText.LastIndexOf(";", pos2);
            int posOpeningParentheses = articleText.LastIndexOf("(", pos2);
            int posOpeningEqualsSign = articleText.LastIndexOf("=", pos2); //[[Hideko Maehata]]
            int posCommaBeforeDate = articleText.LastIndexOf(",", pos2 - 4 - 8); // 4 = len year, 8 : }}, May 1, 1969

            // extra=

            int posCandidate = Math.Max(posSemiColon, posOpeningParentheses);
            posCandidate = Math.Max(posCandidate, posOpeningEqualsSign);
            posCandidate = Math.Max(posCandidate, posCommaBeforeDate);

            if (posCandidate == -1)
                throw new Exception($"Opening sentence {startOpeningSentence}: WTF? No '(' and ';' ?!?1");
            else
            {
                if (posCandidate < pos1)
                    return startOpeningSentence;
                else
                    return articleText.Substring(pos1, posCandidate - (pos1 - "c".Length));  // c = single char. like ; and (
            }
        }

        //private int ResolveBirthDatePrefixPosition(string articleText, int pos1)
        //{
        //    // 'Loop through the prefixes. Thanks to [[Hideko Maehata]]
        //    var prefixes = new string[] {";","(" }; NO: problems with ',' maybe later

        //    int pos2 = -1;
        //    foreach (var prefix in prefixes)
        //    {
        //        int posCandidate = articleText.IndexOf(prefix, pos1 + 1);

        //        if (posCandidate != -1)
        //            if (pos2 == -1)
        //                pos2 = posCandidate;
        //            else
        //                if (posCandidate < pos2)
        //                pos2 = posCandidate;
        //    }
        //    return pos2;
        //}

        private int ResolvePositionYearOfBirth(string articleText, int pos1)
        {
            while (true)
            {
                if (IsNumeric(articleText.Substring(pos1, 4)))
                {
                    int yearOfBirth = int.Parse(articleText.Substring(pos1, 4));
                    if (yearOfBirth > 1875 && yearOfBirth <= 1995 && !IsNumeric(articleText.Substring(pos1 - 1, 1)) && !IsNumeric(articleText.Substring(pos1 + 4, 1)))
                        return pos1 + 1;
                }
                pos1++;

                if (pos1 > 10000)
                    throw new Exception("No year (of birth) found");
            }
        }

        private bool IsNumeric(string value)
        {
            return int.TryParse(value, out _);
        }

        private string RemoveBornPartFromInformation(string information, string linkedName)
        {
            int pos1 = information.IndexOf(" (b.");

            if (pos1 == -1)
                return information;

            int pos2 = information.IndexOf(")", pos1 + 1);
            if (pos1 == -1)
                throw new Exception($" ')' not found after ' (b.'. Article: {linkedName}");

            return information.Substring(0, pos1) + information.Substring(pos2 + ")".Length);
        }

        private string GetAgeAsString(DateTime dateOfBirth, DateTime dateOfDeath, bool yearOfBirthOnly)
        {
            if (yearOfBirthOnly)
            {
                return ResolveAge(new DateTime(dateOfBirth.Year, 12, 31), dateOfDeath).ToString() + "-" +
                       ResolveAge(new DateTime(dateOfBirth.Year, 1, 1), dateOfDeath).ToString();
            }
            else
                return ResolveAge(dateOfBirth, dateOfDeath).ToString();

        }

        private int ResolveAge(DateTime dateOfBirth, DateTime date)
        {
            // Calculate the age.
            var age = date.Year - dateOfBirth.Year;

            // Go back to the year in which the person was born in case of a leap year
            if (dateOfBirth.Date > date.AddYears(-age)) age--;

            return age;
        }

        public IEnumerable<Entry> GetDeceased(DateTime deathDate)
        {
            string text = GetRawTextDeathsPerMonthList(deathDate);

            text = GetDaySection(text, deathDate.Day, false);

            IEnumerable<string> rawDeceased = GetRawDeceased(text);
            IEnumerable<Entry> deceased = rawDeceased.Select(e => ParseEntry(e, deathDate));

            return deceased;
        }

        public IEnumerable<Entry> GetDeceased(int year, int month)
        {
            DateTime deathDate = new DateTime(year, month, 1);
            List<Entry> deceased = new List<Entry>();

            string deathsPerMonthText = GetRawTextDeathsPerMonthList(deathDate);

            for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
            {
                string deathsPerDayText = GetDaySection(deathsPerMonthText, day, false);

                IEnumerable<string> rawDeceased = GetRawDeceased(deathsPerDayText);
                IEnumerable<Entry> deceasedPerDay = rawDeceased.Select(e => ParseEntry(e, new DateTime(year, month, day)));

                deceased.AddRange(deceasedPerDay);
            }

            return deceased;
        }

        private string RemoveRefInfo(string text)
        {
            while (true)
            {
                var pos1 = text.IndexOf("<ref");
                if (pos1 == -1)
                    break;

                // closing tag of <ref ..
                var pos2 = text.IndexOf(">", pos1 + 1);

                if(text.Substring(pos2 - 1, 2) != "/>" )
                    pos2 = text.IndexOf(">", pos2 + 1);

                text = text.Substring(0, pos1) + text.Substring(pos2 + ">".Length);
            }
            return text;
        }

        // Obsolete
        private int ResolveDashPosition(string articleText, int pos1)
        {
            // 'Loop through the dashes..  Asc("-") = 45 btw
            var dashTypes = new string[] { ((char)45).ToString(), "&ndash;", "{{snd}}", "{{spaced ndash}}", "–", ((char)151).ToString() };

            int pos2 = -1;
            foreach (var dashType in dashTypes)
            {
                int posCandidate = articleText.IndexOf(dashType, pos1 + 1);

                if (posCandidate != -1)
                    if (pos2 == -1)
                        pos2 = posCandidate;
                    else
                        if (posCandidate < pos2)
                        pos2 = posCandidate;
            }
            return pos2;
        }

        // #######################################################################################################################################

        private string GetRawTextDeathsPerMonthList(DateTime deathDate)
        {
            string text;
            string month = deathDate.ToString("MMMM", new CultureInfo("en-US"));

            using (WebClient client = new WebClient())                
                // TODO text = client.DownloadString(UrlWikipediaRawBase + $"Deaths_in_{month}_{deathDate.Year}");
                // TODO text = client.DownloadString(UrlWikipediaRawBase + @"User:Mill_1/Months/December");
                text = client.DownloadString(UrlWikipediaRawBase + @"User:Mill_1/tmp");

            text = TrimWikiText(text, month, deathDate.Year);

            text = RemoveSubLists(text);

            CheckEntyPrefixes(text);

            return text;
        }

        private string RemoveSubLists(string text)
        {
            if (!text.Contains(" * *[["))
                return text;

            text = text.Replace("**[[", "~~[[");

            var entries = text.Split('*').Skip(1).ToList();

            entries.ForEach(entry =>
            {
                if (entry.Substring(0, 2) != "[[")
                    text = RemoveSubList(entry, text);
            });

            return text;                
        }

        private string RemoveSubList(string subList, string text)
        {
            int pos = subList.IndexOf("==");

            if (pos == -1)
                throw new InvalidWikipediaPageException($"Invalid markup found: no section found after sub list. Fix the article");

            subList = subList.Substring(0, pos);

            return text.Replace($"*{subList}", string.Empty);
        }

        private void CheckEntyPrefixes(string text)
        {
            if (text.Contains("* "))
                throw new InvalidWikipediaPageException($"Invalid markup style found: '* '. Fix the article");

            text = text.Replace("M*A*S*H", "M+A+S+H");
            text = text.Replace("NOC*NSF", "NOC+NSF");            

            var entries = text.Split('*').Skip(1).ToList();

            // TODO; dit zit niet in de oude master
            entries.ForEach(entry =>
            {
                if (entry.Length == 1)
                    throw new InvalidWikipediaPageException($"Invalid markup content found: '*{entry}*'. Fix the article or the code");

                if (entry.Substring(0, 2) != "[[")
                    throw new InvalidWikipediaPageException($"Invalid markup style found: '*{entry}'. Fix the article or the code");
            });
            // TOT HIER
        }

        public string GetArticleTitle(string nameVersion, int year, int monthId)
        {
            string articleTitle = nameVersion;
            string rawText = GetRawArticleText(ref articleTitle, false);

            if (ContainsValidDeathCategory(rawText, year, monthId))
            {
                Console.WriteLine($"{articleTitle}: SUCCESS");
                return articleTitle;
            }
            else
            {
                // Category Human name disambiguation pages?
                if (IsHumaneNameDisambiguationPage(rawText))
                    return CheckDisambiguationPage(year, monthId, articleTitle, rawText);
                else
                    return null;
            }
        }

        private string GetRawArticleMarkup(ref string article, out bool isRedirect)
        {
            isRedirect = false;
            string rawText = GetRawWikiPageText(article);

            if (rawText.Contains("#REDIRECT"))
            {
                isRedirect = true;
                article = GetRedirectPage(rawText);
            }
            return rawText;
        }

        public string GetRawArticleText(ref string article, bool nettoContent)
        {
            bool isRedirect;

            string rawText = GetRawArticleMarkup(ref article, out isRedirect);            

            if (isRedirect)
                rawText = GetRawWikiPageText(article);           

            if (nettoContent)
                rawText = GetNettoContentRawArticleText(rawText);

            return rawText;
        }

        private string GetNettoContentRawArticleText(string rawText)
        {
            // Article size is an pragmatic yet arbitrary indicator regarding a biography's notability.
            // In the end it cannot be helped that fanboys create large articles for their idols.
            // However, the indicator can be improved by looking at the 'netto content' of the article;
            // Articles can become quite verbose because of the use of infobox-templates and the addition of many categories.
            // Stripping those elements from the markup text results in a more realistic article size.
            int posContentStart = GetContentStart(rawText);

            return rawText.Substring(posContentStart, GetContentEnd(rawText) - posContentStart);
        }

        private int GetContentStart(string rawText)
        {
            int posStart = GetStartPositionInfobox(rawText);

            if (posStart == NoInfobox)
                return 0;

            string infoboxText = GetInfoboxText(rawText);

            return posStart + infoboxText.Length;
        }

        private int GetStartPositionInfobox(string rawText)
        {            
            int pos = rawText.IndexOf("infobox", StringComparison.OrdinalIgnoreCase);

            if (pos == -1)
                return NoInfobox;

            if (rawText.Contains("Please do not add an infobox", StringComparison.OrdinalIgnoreCase))
                return NoInfobox;

            // Find the opening accolades of the listbox
            pos = rawText.LastIndexOf("{{", pos);

            return pos;
        }

        private string GetInfoboxText(string rawText)
        {
            int posStart = GetStartPositionInfobox(rawText);

            // Find the matching closing accolades of the listbox. This is quite tricky.
            int count = 0;
            int posEnd;

            for (posEnd = posStart; posEnd < rawText.Length; posEnd++)
            {
                if (ClosingAccoladesFound(rawText, ref count, posEnd))
                    break;
            }

            return rawText.Substring(posStart, posEnd - posStart + "}}".Length);
        }

        private  bool ClosingAccoladesFound(string rawText, ref int count, int posEnd)
        {
            if (rawText.Substring(posEnd, 2) == "}}")
            {
                if (rawText.Substring(posEnd, 3) == "}}}" && rawText.Substring(posEnd - 1, 3) == "}}}")
                {
                    if (rawText.Substring(posEnd, 4) == "}}}}" && rawText.Substring(posEnd - 2, 4) == "}}}}")
                        count--;
                    //else: centre 2 of 4 consecutive accolades: native_name={{nobold|{{my|Boem}}}}
                }
                else
                    count--;
            }

            if (rawText.Substring(posEnd, 2) == "{{")
            {
                if (rawText.Substring(posEnd, 3) == "{{{" && rawText.Substring(posEnd - 1, 3) == "{{{")
                    //centre 2 of 4 consecutive accolades: you never know..
                    logger.LogDebug($"accolades exception:{rawText.Substring(posEnd, 40)}");
                else
                    count++;
            }

            if (count == 0)
                return true;

            return false;
        }

        private int GetContentEnd(string rawText)
        {
           List<int> posEndList = new List<int>
           {
                rawText.IndexOf("{{Authority control", StringComparison.OrdinalIgnoreCase),
                rawText.IndexOf("{{DEFAULTSORT", StringComparison.OrdinalIgnoreCase),
                rawText.IndexOf("[[Category", StringComparison.OrdinalIgnoreCase)
            };

            posEndList = posEndList.Where(pos => pos != -1).ToList();

            if (posEndList.Count() == 0)
                // If rawText points to ambiguation page then next situation probably occurred:
                // the ambiguation page was created aftr the NYT-json was processed. Fix:
                // Update the db. Example: Gary Jennings (author)
                throw new Exception("Invalid article end. Edit the article");

            int posEnd = posEndList.Min();            

            return posEnd;
        }

        private string CheckDisambiguationPage(int year, int monthId, string articleTitle, string rawText)
        {
            // Do not use regex; error: too many ')'
            string[] searchValues = new string[] { $"–{year})", $"-{year})", $"&ndash;{year})", $"died {year})" };

            string disambiguationEntry = GetDisambiguationEntry(articleTitle, rawText, searchValues);

            if (disambiguationEntry == null)
            {
                if (monthId <= 2)
                {
                    searchValues = new string[] { $"–{year - 1})", $"-{year - 1})", $"&ndash;{year - 1})", $"died {year - 1})" };
                    disambiguationEntry = GetDisambiguationEntry(articleTitle, rawText, searchValues);
                }
            }
            return disambiguationEntry;
        }

        private bool ContainsValidDeathCategory(string rawText, int year, int monthId)
        {
            bool valid;

            valid = rawText.Contains($"[[Category:{year} deaths", StringComparison.OrdinalIgnoreCase);

            if (!valid)
                if (monthId <= 2)
                    valid = rawText.Contains($"[[Category:{year - 1} deaths", StringComparison.OrdinalIgnoreCase);

            return valid;
        }

        private string GetDisambiguationEntry(string articleTitle, string rawText, string[] searchValues)
        {
            string disambiguationEntry = null;

            foreach (string searchValue in searchValues)
            {
                disambiguationEntry = InspectDisambiguationPage(rawText, articleTitle, searchValue);

                if (disambiguationEntry != null)
                {
                    Console.WriteLine($"{disambiguationEntry}: SUCCESS (via disambiguation page)");
                    break;
                }
            }
            return disambiguationEntry;
        }

        public string GetAuthorsArticle(string author, string source)
        {
            string authorsArticle = author;
            string rawText = GetRawArticleTextAuthor(ref authorsArticle);

            if (rawText == null)
                return null;

            if (!rawText.Contains(source))
                return null;

            if (IsHumaneNameDisambiguationPage(rawText))
            {
                authorsArticle = InspectDisambiguationPage(rawText, authorsArticle, source);

                if (authorsArticle == null)
                    return null;
            }

            if (rawText.Contains("journalist") || rawText.Contains("columnist") || rawText.Contains("critic") || rawText.Contains("editor"))
                return authorsArticle;
            else
                return null;
        }

        private string GetRawArticleTextAuthor(ref string authorsArticle)
        {
            try
            {
                return GetRawArticleText(ref authorsArticle, false);
            }
            catch (WikipediaPageNotFoundException)
            {
                return null;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string GetInformationFromRawEntry(string rawEntry)
        {
            string info = rawEntry.Substring(rawEntry.IndexOf("]]") + "]]".Length);

            // Loose the first comma
            info = info.Substring(1).Trim();

            int posRef = info.IndexOf("<ref>");

            if (posRef < 0)
                return info;
            else
                return info.Substring(0, posRef);
        }

        private string GetReferencesFromRawEntry(string rawEntry)
        {
            int posStart = rawEntry.IndexOf("<ref>");

            if (posStart < 0)
                return null;
            else
                return rawEntry.Substring(posStart);
        }

        private string GetNameFromRawEntry (string rawEntry, bool linkedName, out string articleText)
        {
            string namePart = rawEntry.Substring("[[".Length, rawEntry.IndexOf("]]") - "]]".Length);
            int pos = namePart.IndexOf('|');
            string name;            

            if (pos < 0)
                name = namePart;
            else
            {
                if (linkedName)
                    name = namePart.Substring(0, pos);
                else
                    name = namePart.Substring(pos + "|".Length);
            }

            name = CheckRedirection(linkedName, name, out articleText);

            return name;
        }

        private string CheckRedirection(bool linkedName, string name, out string articleText)
        {
            bool isRedirect;

            //if linked name make sure it is not a redirect.
            if (linkedName)
            {
                string originalName = name;

                // TODO: wait what?: met bio text wordt niet gedaan! Waarom ook eigenlijk?
                // GetRawArticleMarkup(ref name, out isRedirect);
                articleText = GetRawArticleMarkup(ref name, out isRedirect);

                string redirectInfo = isRedirect ? $". Corrected REDIRECT '{originalName}'" : string.Empty;

                //Thread.Sleep(100); // TODO lw?
                Console.WriteLine($"Entry: {name}{redirectInfo}");
            }
            else
                articleText = string.Empty;

            return name;
        }

        private IEnumerable<string> GetRawDeceased(string daySection)
        {
            string[] array = daySection.Split(EntryDelimiter);

            IEnumerable<string> rawDeceased = array.Select( e => "[[" +  e);

            return rawDeceased.Skip(1);
        }

        private string GetDaySection(string wikiText, int day, bool trimHeader)
        {
            string daySection = wikiText;
            int pos;

            //Trim left
            pos = Math.Max(daySection.IndexOf($"==={day}==="), daySection.IndexOf($"=== {day} ==="));

            if (pos == -1)
                throw new InvalidWikipediaPageException($"Invalid day section header found. Day: {day}");

            daySection = daySection.Substring(pos);

            if (trimHeader)
                daySection = daySection.Substring(daySection.IndexOf(EntryDelimiter));

            // Trim right
            pos = Math.Max(daySection.IndexOf($"==={day + 1}==="), daySection.IndexOf($"=== {day + 1} ==="));

            if (pos < 0) // we reached the end
                return daySection;
            else
                return daySection.Substring(0, pos);
        }

        private string TrimWikiText(string wikiText, string month, int year)
        {
            string trimmedText = wikiText;
            int pos;

            //Trim left
            pos = Math.Max(trimmedText.IndexOf($"=={month} {year}=="), trimmedText.IndexOf($"== {month} {year} =="));

            if (pos == -1)
                throw new InvalidWikipediaPageException($"Not found:  ==[]{ month } { year}[]== ");

            trimmedText = trimmedText.Substring(pos);

            // Trim right
            pos = Math.Max(trimmedText.IndexOf("==References=="), trimmedText.IndexOf("== References =="));

            if (pos == -1)    
                throw new InvalidWikipediaPageException($"Not found:  ==[]References[]== ");

            trimmedText = trimmedText.Substring(0, pos);

            // Loose '\n'
            trimmedText = trimmedText.Replace("\n", "");

            return trimmedText;
        }

        private bool IsHumaneNameDisambiguationPage(string rawText)
        {
            return rawText.Contains("{{hndis|", StringComparison.OrdinalIgnoreCase) ||
                   rawText.Contains("|hndis}}", StringComparison.OrdinalIgnoreCase) ||
                   rawText.Contains("[[Category: Human name disambiguation pages", StringComparison.OrdinalIgnoreCase);
        }

        private string InspectDisambiguationPage(string rawText, string nameVersion, string searchValue)
        {
            // https://en.wikipedia.org/wiki/Roger_Brown : three entries '-1997)': will be caught in process because of datediff.

            int pos = rawText.IndexOf(searchValue);

            if (pos == -1)
                return null;

            string articleText = rawText.Substring(0, pos + searchValue.Length);

            string disambiguationEntry = GetDisambiguationEntry(articleText);

            if (disambiguationEntry == null)
                return null;

            return CompareArticleWithNameVersion(disambiguationEntry, nameVersion);
        }

        private string GetDisambiguationEntry(string article)
        {
            // look for [[ (reverse)
            int pos = article.LastIndexOf("[[") + 2;
            article = article.Substring(pos);

            // Then look for the first | or ]]
            pos = article.IndexOf("]]");
            int posPipe = article.IndexOf("|") == -1 ? pos++ : article.IndexOf("|");  // probably not necessary..
            pos = Math.Min(pos, posPipe);

            if (pos == -1)    // searchValue within sought article: [[Richard Mason (novelist, 1919–1997)]] 
                return null;

            return article.Substring(0, pos);
        }

        private string CompareArticleWithNameVersion(string article, string nameVersion)
        {
            if (article.Contains(nameVersion, StringComparison.OrdinalIgnoreCase))
                return article;
            else
            {
                // If article consists of three parts; loose the 2nd part and compare again.
                // Number of occurrences do not warrant application of fuzzy string comparisons.
                string[] parts = article.Split(" ");

                if (parts.Length != 3)
                    return null;
                else
                {
                    if ($"{parts[0]} {parts[2]}".Contains(nameVersion, StringComparison.OrdinalIgnoreCase))
                        return article;
                    else
                        return null;
                }
            }
        }       

        private string GetRawWikiPageText(string wikiPage)
        {
            string uri = UrlWikipediaRawBase + wikiPage.Replace(" ", "_");

            try
            {
                using WebClient client = new WebClient();
                return client.DownloadString(uri);
            }
            catch (WebException) // article does not exist (anymore) in Wikipedia
            {
                throw new WikipediaPageNotFoundException($"{wikiPage}: FAIL: no such wiki page");
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string GetRedirectPage(string rawText)
        {
            // #REDIRECT[[Robert McG. Thomas Jr.]]
            int pos = rawText.IndexOf("[[");
            string redirectPage = rawText.Substring(pos + 2);
            pos = redirectPage.IndexOf("]]");

            return redirectPage.Substring(0, pos);
        }
    }
}
