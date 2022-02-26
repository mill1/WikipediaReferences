using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using Wikimedia.Utilities.Interfaces;
using Wikimedia.Utilities.Exceptions;
using WikipediaReferences.Interfaces;

namespace WikipediaReferences.Services
{
    public class WikipediaService : IWikipediaService
    {
        private const int NoInfobox = -1;
        private readonly IWikiTextService wikiTextService;
        private readonly IWikipediaWebClient wikipediaWebClient;
        private readonly ILogger logger;

        public WikipediaService(IWikiTextService wikiTextService, IWikipediaWebClient wikipediaWebClient, ILogger<WikipediaService> logger)
        {
            this.wikiTextService = wikiTextService;
            this.wikipediaWebClient = wikipediaWebClient;
            this.logger = logger;
        }

        public IEnumerable<Entry> GetDeceased(DateTime deathDate)
        {
            // TODO
            const string UrlWikipediaRawBase = "https://en.wikipedia.org/w/index.php?action=raw&title=";        
            // text = client.DownloadString(UrlWikipediaRawBase + @"User:Mill_1/Months/December");
            // text = client.DownloadString(UrlWikipediaRawBase + @"User:Mill_1/Sandbox2");
            string text = wikiTextService.GetWikiTextDeathsPerMonth(deathDate, true, UrlWikipediaRawBase + @"User:Mill_1/Sandbox2");

            text = GetDaySection(text, deathDate.Day, false);

            IEnumerable<string> rawDeceased = GetDeceasedTextAsList(text);
            IEnumerable<Entry> deceased = rawDeceased.Select(e => ParseEntry(e, deathDate));

            return deceased;
        }

        public IEnumerable<Entry> GetDeceased(int year, int monthId)
        {
            DateTime deathDate = new DateTime(year, monthId, 1);
            List<Entry> deceased = new List<Entry>();

            string deathsPerMonthText = GetWikiTextDeathsPerMonth(deathDate);

            for (int day = 1; day <= DateTime.DaysInMonth(year, monthId); day++)
            {
                string deathsPerDayText = GetDaySection(deathsPerMonthText, day, false);

                IEnumerable<string> rawDeceased = GetDeceasedTextAsList(deathsPerDayText);
                IEnumerable<Entry> deceasedPerDay = rawDeceased.Select(e => ParseEntry(e, new DateTime(year, monthId, day)));

                deceased.AddRange(deceasedPerDay);
            }

            return deceased;
        }

        private Entry ParseEntry(string entryText, DateTime deathDate)
        {
            return new Entry
            {
                LinkedName = GetNameFromEntryText(entryText, true),
                Name = GetNameFromEntryText(entryText, false),
                Information = wikiTextService.GetInformationFromEntryText(entryText),
                Reference = wikiTextService.GetReferencesFromEntryText(entryText),
                DeathDate = deathDate
            };
        }

        public string GetArticleTitle(string nameVersion, int year, int monthId)
        {
            string articleTitle = nameVersion;
            string rawText = GetRawArticleText(articleTitle, false);

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

        public string GetRawArticleText(string article, bool nettoContent)
        {
            string rawText = wikipediaWebClient.GetWikiTextArticle(article, out _);

            if (nettoContent)
                rawText = GetNettoContentRawArticleText(rawText);

            return rawText;
        }

        private string GetNettoContentRawArticleText(string rawText)
        {
            // Article size is an pragmatic yet arbitrary indicator regarding a biography's notability.
            // In the end it cannot be helped that fanboys create large articles for their idols.
            // However, the indicator can be improved somewhat by looking at the 'netto content' of the article:
            // Articles can become quite verbose because of the use of infobox-templates and the addition of many categories.
            // Stripping those elements from the markup text results in a more realistic article size.
            int posContentStart = GetContentStart(rawText);
            int posContentEnd = GetContentEnd(rawText);

            return rawText.Substring(posContentStart, posContentEnd - posContentStart);
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

        private bool ClosingAccoladesFound(string rawText, ref int count, int posEnd)
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

            if (!posEndList.Any())
            {
                int pos = rawText.IndexOf("''' may refer to");

                if (pos == -1)
                    throw new InvalidWikipediaPageException("Invalid article end. Edit the article");
                else
                {
                    // If rawText points to disambiguation page then next situation probably occurred:
                    // the disambiguation page was created aftr the NYT-json was processed. Fix:
                    // Update the db. Example: Gary Jennings (author) or Bob Flanagan (author)
                    var disambiguation = rawText.Substring(0, pos + 3); // = '''
                    throw new WikipediaPageNotFoundException($"Matched article name is now part of a disambiguation page: {disambiguation}... Update the db with the new WP name");
                }

            }

            int posEnd = posEndList.Min();

            return posEnd;
        }

        private string CheckDisambiguationPage(int year, int monthId, string articleTitle, string rawText)
        {
            // Do not use regex; error: too many ')'
            string[] searchValues = new string[] { $"–{year})", $"-{year})", $"&ndash;{year})", $"died {year})" };

            string disambiguationEntry = GetDisambiguationEntry(articleTitle, rawText, searchValues);

            if (disambiguationEntry == null && monthId <= 2)
            {
                searchValues = new string[] { $"–{year - 1})", $"-{year - 1})", $"&ndash;{year - 1})", $"died {year - 1})" };
                disambiguationEntry = GetDisambiguationEntry(articleTitle, rawText, searchValues);
            }
            return disambiguationEntry;
        }

        private bool ContainsValidDeathCategory(string rawText, int year, int monthId)
        {
            bool valid;

            valid = rawText.Contains($"[[Category:{year} deaths", StringComparison.OrdinalIgnoreCase);

            if (!valid && monthId <= 2)
                valid = rawText.Contains($"[[Category:{year - 1} deaths", StringComparison.OrdinalIgnoreCase);

            return valid;
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
                return GetRawArticleText(authorsArticle, false);
            }
            catch (WikipediaPageNotFoundException)
            {
                return null;
            }
        }

        private IEnumerable<string> GetDeceasedTextAsList(string daySection)
        {
            string[] array = daySection.Split("*[[");

            IEnumerable<string> rawDeceased = array.Select(e => "[[" + e);

            return rawDeceased.Skip(1);
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
    }
}
