using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WikipediaReferences.Interfaces;

namespace WikipediaReferences.Services
{
    public class WikipediaService : IWikipediaService
    {
        private const string UrlWikipediaRawBase = "https://en.wikipedia.org/w/index.php?action=raw&title="; // title=Deaths_in_May_2005
        private const string EntryDelimiter = "*[[";

        public IEnumerable<Entry> GetDeceased(DateTime deathDate)
        {
            string text;
            string month = deathDate.ToString("MMMM", new CultureInfo("en-US"));

            using (WebClient client = new WebClient())
                text = client.DownloadString(UrlWikipediaRawBase + $"Deaths_in_{month}_{deathDate.Year}");

            if (text.Contains("* [[")) // We only want '*[[' (without the space); edit article in that case
                throw new Exception("Invalid markup style found: * [[");

            text = TrimWikiText(text, month, deathDate.Year);
            text = GetDaySection(text, deathDate.Day, false);

            IEnumerable<string> rawDeceased = GetRawDeceased(text);
            IEnumerable<Entry> deceased = rawDeceased.Select(e => ParseEntry(e, deathDate));

            return deceased;
        }

        public string GetArticleTitle(string nameVersion, int year)
        {
            string biography = nameVersion;
            string rawText = GetRawArticleText(ref biography, true);

            // TODO: in januari ook year 1996
            if (rawText.Contains($"[[Category:{year} deaths", StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{biography}: SUCCESS");
                Console.ResetColor();
                return biography;
            }
            else
            {
                // Category Human name disambiguation pages?
                if (IsHumaneNameDisambiguationPage(rawText))
                {
                    // Do not use regex; error: too many ')'
                    string[] searchValues = new string[] { $"–{year})", $"-{year})", $"&ndash;{year})", $"died {year})" };

                    foreach (string searchValue in searchValues)
                    {
                        string disambiguationEntry = InspectDisambiguationPage(rawText, biography, searchValue);

                        if (disambiguationEntry != null)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"{disambiguationEntry}: SUCCESS (via disambiguation page)");
                            Console.ResetColor();
                            return disambiguationEntry;
                        }
                    }
                    return null;
                }
                else
                    return null;
            }
        }

        public string GetAuthorsArticle(string author)
        {
            // TODO
            const string NYT = "The New York Times";

            string authorsArticle = author;
            string rawText = GetRawArticleText(ref authorsArticle, false);

            if (!rawText.Contains(NYT))
                return null;

            if (IsHumaneNameDisambiguationPage(rawText))
            {
                authorsArticle = InspectDisambiguationPage(rawText, authorsArticle, NYT);

                if (authorsArticle == null)
                    return null;
            }

            if (rawText.Contains("journalist") ||
                rawText.Contains("columnist") ||
                rawText.Contains("critic") ||
                rawText.Contains("editor"))
            {
                return authorsArticle;
            }
            else
                return null;
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

            string article = rawText.Substring(0, pos + searchValue.Length);

            // look for [[ (reverse)
            pos = article.LastIndexOf("[[") + 2;
            article = article.Substring(pos);

            // Then look for the first | or ]]
            pos = article.IndexOf("]]");
            int posPipe = article.IndexOf("|") == -1 ? pos++ : article.IndexOf("|");  // probably not necessary..
            pos = Math.Min(pos, posPipe);

            if (pos == -1)    // searchValue within sought article: [[Richard Mason (novelist, 1919–1997)]] 
                return null;

            article = article.Substring(0, pos);

            return CompareArticleWithNameVersion(article, nameVersion);
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

        private string GetRawArticleText(ref string article, bool printNotFound)
        {
            string rawText = GetArticleText(article, printNotFound);

            if (rawText.Contains("#REDIRECT"))
            {
                article = GetRedirectPage(rawText);
                rawText = GetArticleText(article, printNotFound);
            }

            return rawText;
        }

        private string GetArticleText(string article, bool printNotFound)
        {
            string address;
            article = article.Replace(" ", "_");

            address = @"https://en.wikipedia.org/w/index.php?action=raw&title=" + article;

            try
            {
                return GetTextFromUrl(address);
            }
            catch (WebException e)
            {
                // article does not exist in Wikipedia
                if (printNotFound)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{article.Replace("_", " ")}: FAIL (no such article)");
                    Console.ResetColor();
                }
                return string.Empty;
            }
            catch (Exception)
            {
                throw;
            }
        }

        // TODO centraliseren
        private string GetTextFromUrl(string address)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(address);
            //HttpWebRequest httpWebRequest = WebRequest.CreateHttp(address);
            httpWebRequest.Method = "GET";
            httpWebRequest.Headers.Add("User-Agent", "PostmanRuntime / 7.26.1");

            using (WebResponse response = httpWebRequest.GetResponse())
            {
                HttpWebResponse httpResponse = response as HttpWebResponse;
                using (StreamReader reader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
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
