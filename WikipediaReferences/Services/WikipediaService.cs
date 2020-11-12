using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using WikipediaReferences.Interfaces;

namespace WikipediaReferences.Services
{
    public class WikipediaService : IWikipediaService
    {
        private const string UrlWikipediaRawBase = "https://en.wikipedia.org/w/index.php?action=raw&title=";
        private const string EntryDelimiter = "*[[";

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

        private Entry ParseEntry(string rawEntry, DateTime deathDate)
        {
            return new Entry
            {
                LinkedName = GetNameFromRawEntry(rawEntry, true),
                Name = GetNameFromRawEntry(rawEntry, false),
                Information = GetInformationFromRawEntry(rawEntry),
                Reference = GetReferenceFromRawEntry(rawEntry),
                DeathDate = deathDate
            };
        }

        private string GetRawTextDeathsPerMonthList(DateTime deathDate)
        {
            string text;
            string month = deathDate.ToString("MMMM", new CultureInfo("en-US"));

            using (WebClient client = new WebClient())
                text = client.DownloadString(UrlWikipediaRawBase + $"Deaths_in_{month}_{deathDate.Year}");

            if (text.Contains("* [[")) // We only want '*[[' (without the space); edit article in that case.
                throw new Exception("Invalid markup style found: * [[");

            text = TrimWikiText(text, month, deathDate.Year);
            return text;
        }

        public string GetArticleTitle(string nameVersion, int year, int monthId)
        {
            string articleTitle = nameVersion;
            string rawText = GetRawArticleText(ref articleTitle, false, true);

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
        public string GetRawArticleText(ref string article, bool nettoContent, bool printNotFound)
        {
            string rawText = GetRawWikiPageText(article, printNotFound);

            if (rawText.Contains("#REDIRECT"))
            {
                article = GetRedirectPage(rawText);
                rawText = GetRawWikiPageText(article, printNotFound);
            }

            if (nettoContent)
                rawText = GetNettoContentRawArticleText(rawText);

            return rawText;
        }

        private string GetNettoContentRawArticleText(string rawText)
        {
            return "jaja";
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
            string rawText = GetRawArticleText(ref authorsArticle, false, false);

            if (!rawText.Contains(source))
                return null;

            if (IsHumaneNameDisambiguationPage(rawText))
            {
                authorsArticle = InspectDisambiguationPage(rawText, authorsArticle, source);

                if (authorsArticle == null)
                    return null;
            }

            if (rawText.Contains("journalist") || rawText.Contains("columnist") || rawText.Contains("critic") ||  rawText.Contains("editor"))
                return authorsArticle;
            else
                return null;
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

        private string GetReferenceFromRawEntry(string rawEntry)
        {
            int pos = rawEntry.IndexOf("<ref>");

            if (pos < 0)
                return null;
            else
                return rawEntry.Substring(pos);
        }

        private string GetNameFromRawEntry (string rawEntry, bool linkedName)
        {
            string namePart = rawEntry.Substring("[[".Length, rawEntry.IndexOf("]]") - "]]".Length);
            int pos = namePart.IndexOf('|');

            if (pos < 0)
                return namePart;
            else
            {
                if (linkedName)
                    return namePart.Substring(0, pos);
                else
                    return namePart.Substring(pos + "|".Length);
            }
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
            trimmedText = trimmedText.Substring(pos);

            // Trim right
            pos = Math.Max(trimmedText.IndexOf("==References=="), trimmedText.IndexOf("== References =="));
            trimmedText = trimmedText.Substring(0, pos);

            // Loose '\n'
            trimmedText = trimmedText.Replace("\n", "");

            return trimmedText;
        }

        private bool IsHumaneNameDisambiguationPage(string rawText)
        {
            return rawText.Contains("{{hndis|") ||
                   rawText.Contains("|hndis}}") ||
                   rawText.Contains("[[Category: Human name disambiguation pages", StringComparison.OrdinalIgnoreCase);
        }

        private string InspectDisambiguationPage(string rawText, string nameVersion, string searchValue)
        {
            // TODO https://en.wikipedia.org/wiki/Roger_Brown : three entries '-1997)'

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

        private string GetRawWikiPageText(string wikiPage, bool printNotFound)
        {
            string uri = UrlWikipediaRawBase + wikiPage.Replace(" ", "_");

            try
            {
                using WebClient client = new WebClient();
                return client.DownloadString(uri);
            }
            catch (WebException) // article does not exist in Wikipedia
            {                
                if (printNotFound)
                    Console.WriteLine($"{wikiPage}: FAIL (no such wiki page)");

                return string.Empty;
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
