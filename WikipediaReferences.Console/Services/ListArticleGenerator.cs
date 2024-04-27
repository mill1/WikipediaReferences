using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Wikimedia.Utilities.Exceptions;
using Wikimedia.Utilities.Interfaces;
using WikipediaReferences.Models;

namespace WikipediaReferences.Console.Services
{
    public class ListArticleGenerator
    {
        private readonly IConfiguration configuration;
        private readonly Util util;
        private readonly ArticleAnalyzer articleAnalyzer;
        private readonly IToolforgeService toolforgeService;               

        public ListArticleGenerator(IConfiguration configuration, Util util, ArticleAnalyzer articleAnalyzer, IToolforgeService toolforgeService)
        {
            this.configuration = configuration;
            this.util = util;
            this.articleAnalyzer = articleAnalyzer;
            this.toolforgeService = toolforgeService;
        }

        public void PrintDeathsPerMonthArticle(int year, int monthId)
        {
            try
            {
                UI.Console.WriteLine("New article?  (y/n, q to quit)");
                string newArticle = UI.Console.ReadLine();

                if (newArticle.Equals("q", StringComparison.OrdinalIgnoreCase))
                    return;

                string articleTitle = GetArticleTitle(year, monthId, newArticle);

                UI.Console.WriteLine($"Fetching the entries from article {articleTitle}...");
                var entries = GetEntriesPermonth(year, monthId, articleTitle);

                if (ArticleContainsDuplicates(entries, out string duplicateLinkedName))
                {
                    UI.Console.WriteLine($"Article contains duplicate entry: {duplicateLinkedName}. Address it.");
                    return;
                }

                EvaluateDeathsPerMonthArticle(year, monthId, entries);
                CheckIfArticleContainsSublist(articleTitle);
                CheckIfArticleContainsUnknownDateSection(entries);

                UI.Console.WriteLine("\r\nEvaluation complete. Continue? (y/n)");

                if (UI.Console.ReadLine().Equals("n", StringComparison.OrdinalIgnoreCase))
                    return;

                PrintOutput(year, monthId, entries);

                UI.Console.WriteLine($"List generated. See folder:\r\n{Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "output")}");
            }
            catch (WikipediaReferencesException e)
            {
                UI.Console.WriteLine(ConsoleColor.Magenta, e.Message);
            }
            catch (Exception e)
            {
                UI.Console.WriteLine(ConsoleColor.Red, e);
            }
        }

        public void PrintDpmFromDpy(int year, int monthId)
        {
            try
            {
                string articleTitle = year >= 1988 ? $"Deaths in {year}" : EscapeCharacters( $"User:Braintic/Deaths in {year}");

                UI.Console.WriteLine($"Fetching the entries from article {articleTitle}...");

                string uri = $"wikipedia/rawarticle/{articleTitle}/netto/false";
                HttpResponseMessage response = util.SendGetRequest(uri);

                string rawArticleText = util.HandleResponse(response, articleTitle);

                string monthSection = GetMonthSectionFromRawText(rawArticleText, monthId);

                //Sanitize
                monthSection = monthSection.Replace("* [", "*[");
                monthSection = monthSection.Replace("*  [", "*[");
                monthSection = monthSection.Replace("-born " , "-");

                string monthName = GetMonthNames().ElementAt(monthId - 1);
                monthSection = TranformToFormatDpm(year, monthId, monthSection, monthName);

                PrintOutput(year, monthName, monthSection);

                UI.Console.WriteLine($"List generated. See folder:\r\n{Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "output")}");
            }
            catch (Exception e)
            {
                UI.Console.WriteLine(ConsoleColor.Red, e);
            }
        }

        private string TranformToFormatDpm(int year, int monthId, string monthSection, string monthName)
        {
            monthSection = LooseFiles(monthName, monthSection);
            monthSection = LooseComments(monthSection);

            for (int day = 1; day <= DateTime.DaysInMonth(year, monthId); day++)
            {
                string oldValue = $"*[[{monthName} {day}]]";
                monthSection = monthSection.Replace(oldValue, $"\r\n==={day}===");
            }

            monthSection = monthSection.Replace($"*[[{monthName}]] (unknown date)", "\r\n===Unknown date===");
            monthSection = monthSection.Replace($"*[[{monthName}]] (date unknown)", "\r\n===Unknown date===");
            // line feeds
            monthSection = monthSection.Replace(@"\n", "\n");
            // no sublist
            monthSection = monthSection.Replace("**[[", "*[[");

            int posStart = monthSection.IndexOf("**");
            if (posStart != -1) 
            {
                // wiki link is preceded by 'Sir', 'Madam' et cetera. Fix it by including it in the wiki link: [[Elton John|Sir Elton John]].
                int posEnd = monthSection.IndexOf("]]", posStart);
                string invalidEntry = monthSection.Substring(posStart + 2, posEnd - posStart).Trim();
                throw new InvalidWikipediaPageException($"\r\nInvalid entry encountered: {invalidEntry}. Correct it.");
            }; 

            return monthSection;
        }        

        private string LooseFiles(string monthName, string monthSection)
        {
            var firstDay = $"*[[{monthName} 1]]";

            int pos = monthSection.IndexOf(firstDay);

            if (pos == -1)
                throw new InvalidWikipediaPageException($"\r\nFirst day part not found: '{firstDay}'");

            return monthSection.Substring(pos);
        }

        private string LooseComments(string wikiText)
        {
            while (true)
            {
                int posStart = wikiText.IndexOf("<!--");

                if (posStart == -1)
                    break;

                int posEnd = wikiText.IndexOf("-->", posStart);

                if (posEnd == -1)
                    throw new Exception("No matching end tag '-->' found!");

                string remove = wikiText.Substring(posStart, posEnd - posStart + "-->".Length);

                wikiText = wikiText.Replace(remove, string.Empty);
            };

            return wikiText;
        }

        private void PrintOutput(int year, string monthName, string monthSection)
        {
            string fileName = $"Deaths in {monthName} {year}.txt";
            string file = Path.Combine("output", fileName);

            using (var writer = File.CreateText(file))
            {
                writer.WriteLine($"=={monthName} {year}==");
                writer.Write(monthSection);
            }
        }        

        private string GetMonthSectionFromRawText(string rawArticleText, int monthId)
        {
            string currentMonthName = GetMonthNames().ElementAt(monthId - 1);

            int posStart = rawArticleText.IndexOf($"==={currentMonthName}===");

            if (posStart == -1)
                throw new InvalidWikipediaPageException($"\r\nMonth section not found: '==={currentMonthName}==='");

            string nextSection = monthId == 12 ? "==References==" : $"==={GetMonthNames().ElementAt(monthId)}===";

            int posEnd = rawArticleText.IndexOf(nextSection);

            if (posEnd == -1)
                throw new InvalidWikipediaPageException($"\r\nNext section not found: '{nextSection}'");

            return rawArticleText.Substring(posStart, posEnd - posStart).Trim();
        }

        private string GetArticleTitle(int year, int monthId, string newArticle)
        {
            string articleTitle;

            if (newArticle == "y")
            {
                articleTitle = configuration.GetValue<string>("New list article source");
                // TODO uitzoeken waarom configuration.GetValue niet meer werkt..
                articleTitle = articleTitle ?? "User:Mill 1/Months/December";
                articleTitle = EscapeCharacters(articleTitle);
            }
            else
            {
                articleTitle = $"Deaths in {GetMonthNames().ElementAt(monthId - 1)} {year}";
            }
            return articleTitle;
        }

        private static string EscapeCharacters(string text)
        {
            text = text.Replace(" ", "_");
            text = text.Replace(":", "%3A");
            text = text.Replace("/", "%2F");
            return text;
        }

        private void CheckIfArticleContainsSublist(string articleTitle)
        {

            bool articleContainsSublist = articleAnalyzer.ArticleContainsSublist(articleTitle);

            if (articleContainsSublist)
                UI.Console.Write(ConsoleColor.Magenta, "\r\nATTENTION! Sublist(s) in article are not processed (yet)!");
        }

        private void CheckIfArticleContainsUnknownDateSection(IEnumerable<Entry> entries)
        {

            bool articleContainsUnknownDateSection = entries.Where(e => e.Information.Contains("===Unknown date===")).Any();

            if (articleContainsUnknownDateSection)
                UI.Console.Write(ConsoleColor.Magenta, "\r\nATTENTION! Unknown date section present. Check bottom entries!");
        }

        private bool ArticleContainsDuplicates(IEnumerable<Entry> entries, out string duplicateLinkedName)
        {
            var duplicates = entries.GroupBy(x => x.LinkedName)
              .Where(g => g.Count() > 1)
              .Select(y => y.Key);

            if (duplicates.Any())
            {
                duplicateLinkedName = duplicates.First();
                return true;
            }
            else
            {
                duplicateLinkedName = null;
                return false;
            }

        }

        private void EvaluateDeathsPerMonthArticle(int year, int monthId, IEnumerable<Entry> entries)
        {
            UI.Console.WriteLine("Evaluating the entries...");

            var references = GetReferencesPermonth(year, monthId);

            for (int day = 1; day <= DateTime.DaysInMonth(year, monthId); day++)
            {
                UI.Console.WriteLine($"\r\nChecking nyt ref. date {new DateTime(year, monthId, day).ToShortDateString()}");

                IEnumerable<Reference> referencesPerDay = references.Where(r => r.DeathDate.Day == day);

                foreach (var reference in referencesPerDay)
                    HandleReference(reference, entries);
            }
        }

        private void PrintOutput(int year, int monthId, IEnumerable<Entry> entries)
        {
            string month = GetMonthNames().ElementAt(monthId - 1);

            Directory.CreateDirectory("output");
            string fileName = $"Deaths in {month} {year}.txt";
            string file = Path.Combine("output", fileName);

            int day = 0;

            using (var writer = File.CreateText(file))
            {
                writer.WriteLine($"=={month} {year}==");

                foreach (var entry in entries)
                {
                    if (entry.DeathDate.Day != day)
                    {
                        day = entry.DeathDate.Day;
                        writer.WriteLine($"\r\n==={day}===");
                    }
                    writer.WriteLine(entry.ToString());
                }
            }
        }

        private void HandleReference(Reference reference, IEnumerable<Entry> entries)
        {
            //Get matching entry 
            Entry entry = entries.FirstOrDefault(e => e.LinkedName == reference.ArticleTitle);

            if (entry == null)
            {
                var directLinks = toolforgeService.GetWikilinksInfo(reference.ArticleTitle).direct;
                // TODO uitzoeken waarom configuration.GetValue niet meer werkt
                int minimumNumberOfLinksToArticle = int.Parse(configuration.GetValue<string>("Minimum number of links to article") ?? "24");


                if (directLinks >= minimumNumberOfLinksToArticle)
                    UI.Console.WriteLine(ConsoleColor.Magenta, $"{reference.ArticleTitle} not in day subsection. (# of links: {directLinks})");
            }
            else
                HandleExistingEntry(entry, reference);
        }

        public void DetermineNumberOfCharactersBiography()
        {
            try
            {
                //Determine netto nr of chars of article
                UI.Console.WriteLine("Article title:");
                string articleTitle = UI.Console.ReadLine();

                int numberOfChars = GetNumberOfCharactersBiography(articleTitle, netto: true);

                UI.Console.WriteLine(ConsoleColor.Green, $"Number of netto chars: {numberOfChars}");
            }
            catch (WikipediaReferencesException e)
            {
                UI.Console.WriteLine(ConsoleColor.Blue, e.Message);
            }
        }

        private void HandleExistingEntry(Entry entry, Reference reference)
        {
            if (entry.DeathDate == reference.DeathDate)
            {
                ConsoleColor consoleColor;
                string refInfo = HandleMatchingDatesOfDeath(entry, reference, out consoleColor);

                UI.Console.WriteLine(consoleColor, $"{entry.LinkedName}: {refInfo}");
            }
            else
                PrintMismatchDateOfDeaths(reference, entry);
        }

        private string HandleMatchingDatesOfDeath(Entry entry, Reference reference, out ConsoleColor consoleColor)
        {
            if (entry.Reference == null)
            {
                // Entry without reference found for which a NYT obit. exists. Set access date of reference to add to today.
                reference.AccessDate = DateTime.Today;
                entry.Reference = reference.GetNewsReference();
                consoleColor = ConsoleColor.Green;
                return "Added NYT reference!";
            }
            else
            {
                if (entry.Reference.Contains("nytimes.com/") && !entry.Reference.Contains("paid notice", StringComparison.OrdinalIgnoreCase))
                {
                    // Entry has NYT obituary reference. Update the reference but keep the access date of the original ref.                    
                    reference.AccessDate = GetAccessDateFromEntryReference(entry.Reference, reference.AccessDate);
                    entry.Reference = reference.GetNewsReference();
                    consoleColor = ConsoleColor.DarkYellow;
                    return $"Updated NYT reference.";
                }
                else
                {
                    if (KeepExistingReference(entry.Reference))
                    {
                        consoleColor = ConsoleColor.DarkGray;
                        return "Existing source is ok.";
                    }
                    else
                    {
                        reference.AccessDate = DateTime.Today;
                        entry.Reference = reference.GetNewsReference();
                        consoleColor = ConsoleColor.Green;
                        return "Replaced with NYT reference.";
                    }
                }
            }
        }

        private bool KeepExistingReference(string reference)
        {
            if (reference.Contains("latimes.com/") ||
                reference.Contains("independent.co.uk/") ||
                reference.Contains("news.bbc.co.uk/") ||
                reference.Contains("telegraph.co.uk/") ||
                reference.Contains("washingtonpost.com/") ||
                reference.Contains("rollingstone.com/") ||
                reference.Contains("economist.com/") ||
                reference.Contains("irishtimes.com/") ||
                reference.Contains("britannica.com/") ||
                reference.Contains("theguardian.com/"))
                return true;

            // <ref>[http..
            if (!reference.Contains("{{cite", StringComparison.OrdinalIgnoreCase) && !reference.Contains("{{citation", StringComparison.OrdinalIgnoreCase))
                return false;

            // Other news, web citations
            if (reference.Contains("{{cite news", StringComparison.OrdinalIgnoreCase) || reference.Contains("{{cite web", StringComparison.OrdinalIgnoreCase) || reference.Contains("{{citation", StringComparison.OrdinalIgnoreCase))
                return false;
            else
                // books and journals are preferable over news sources
                return true;
        }

        private void PrintMismatchDateOfDeaths(Reference reference, Entry entry)
        {
            string message = $"Death date entry: {entry.DeathDate.ToString("dd-MM-yyyy")} Url:\r\n{reference.Url}";

            if (entry.Reference == null)
                UI.Console.WriteLine(ConsoleColor.Red, $"{entry.LinkedName}: New NYT reference! {message}");
            else
                if (entry.Reference.Contains("New York Times"))
                UI.Console.WriteLine(ConsoleColor.Red, $"{entry.LinkedName}: Update NYT reference! {message}");
        }

        private DateTime GetAccessDateFromEntryReference(string entryReference, DateTime defaultAccessDate)
        {
            int posStart = entryReference.IndexOf("access-date");

            if (posStart == -1)
                posStart = entryReference.IndexOf("accessdate");

            if (posStart == -1)
                return defaultAccessDate;

            posStart = entryReference.IndexOf("=", posStart) + 1;

            try
            {
                int posEnd = entryReference.IndexOf("|", posStart);

                if (posEnd == -1)
                    posEnd = entryReference.IndexOf("}}", posStart);

                string accessdate = entryReference.Substring(posStart, posEnd - posStart).Trim();

                return DateTime.Parse(accessdate);
            }
            catch (Exception)
            {
                return defaultAccessDate;
            }
        }

        private int GetNumberOfCharactersBiography(string articleTitle, bool netto)
        {
            articleTitle = articleTitle.Replace("/", "%2F"); // https://en.wikipedia.org/wiki/Bob_Carroll_(singer/actor)

            // page redirects have been handled
            string uri = $"wikipedia/rawarticle/{articleTitle}/netto/{netto}";
            HttpResponseMessage response = util.SendGetRequest(uri);

            string rawArticleText = util.HandleResponse(response, articleTitle);

            return rawArticleText.Length;
        }

        private IEnumerable<Entry> GetEntriesPermonth(int year, int monthId, string articleTitle)
        {
            IEnumerable<Entry> entriesPerMonth;
            string uri = $"wikipedia/deceased/{year}/{monthId}/{articleTitle}";
            HttpResponseMessage response = util.SendGetRequest(uri);

            string result = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
                entriesPerMonth = JsonConvert.DeserializeObject<IEnumerable<Entry>>(result);
            else
            {
                if (result.Contains(typeof(WikipediaPageNotFoundException).Name))
                    throw new WikipediaReferencesException($"\r\nRedlink entry in the deaths per month article. Remove it.");
                else
                {
                    if (result.Contains(typeof(InvalidWikipediaPageException).Name))
                        throw new WikipediaReferencesException(result);
                    else
                        throw new ArithmeticException(result);
                }
            }

            return entriesPerMonth;
        }

        private IEnumerable<Reference> GetReferencesPermonth(int year, int monthId)
        {
            IEnumerable<Reference> references;
            string uri = $"nytimes/references/{year}/{monthId}";
            HttpResponseMessage response = util.SendGetRequest(uri);
            string result = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
                references = JsonConvert.DeserializeObject<IEnumerable<Reference>>(result);
            else
                throw new WikipediaReferencesException(result);

            return references;
        }

        private List<string> GetMonthNames()
        {
            List<string> monthNames;

            monthNames = CultureInfo.GetCultureInfo("en-US").DateTimeFormat.MonthNames.ToList();
            //Trunc 13th month
            monthNames.RemoveAt(monthNames.Count - 1);

            return monthNames;
        }
    }
}
