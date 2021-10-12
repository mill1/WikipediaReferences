using System;
using System.Collections.Generic;
using System.Net.Http;
using WikipediaReferences;
using System.Text;
using WikipediaReferences.Models;
using System.Linq;
using Newtonsoft.Json;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace WikipediaConsole.Services
{
    public class ListArticleGenerator
    {
        private readonly int minimumNrOfNettoCharsBiography;

        private readonly Util util;
        private readonly ArticleAnalyzer articleAnalyzer;

        private IEnumerable<Entry> entries;
        private  IEnumerable<Reference> references;

        public ListArticleGenerator(IConfiguration configuration, Util util, ArticleAnalyzer articleAnalyzer)
        {
            this.util = util;
            this.articleAnalyzer = articleAnalyzer;

            minimumNrOfNettoCharsBiography = int.Parse(configuration.GetValue<string>("Minimum number of netto chars wiki biography"));
        }

        /*
         *  TMP CODE regarding the 1995 month articles
         * 
            STEPS:
            -----------------------------------
            Phase 1. Cleaning the article
            -----------------------------------
            - Initialize page https://en.wikipedia.org/wiki/User:Mill_1/tmp : paste contents main section of https://en.wikipedia.org/wiki/Deaths_in_January_1995 (so WITHOUT CATEGORIES)

            Manual changes:
            - Remove images:                        [[File:Wigner.jpg|thumb|120px|[[Eugene Wigner]]]]
            - Remove entries with foreign article:  search for '*[[:'    *[[:de:George Eells|George Eells]], American writer and editor 
            - Remove unknown day section

            Automated changes:    
            - In this app select menu option '1' 
            This will generate the cleaned article in text file (see DEV):
            ..\netcoreapp3.1\output1995\Deaths in [month] 1995.txt
            - Paste the contents of the file in https://en.wikipedia.org/wiki/User:Mill_1/tmp and analyse the diffs.

            DEV:
            - Standardize entry prefix: '* '  ->  '*'  loose trailing space for both '* [[Entry]]' and '* Entry'
            - Entries without article:         *Kurt Lindner, German born American mutual funds manager (b. 1912)             
            - All the cn's: overkill           {{citation needed|date=June 2021}}

            -----------------------------------
            Phase 2. Fixing the article
            -----------------------------------
            Fix the entry format:            * [[Fred West]], English serial killer (b. 1941)<ref>..</ref>  ->  * [[Fred West]], 65, English serial killer.<ref>..</ref>
            - In this app select menu option '2' 
            This will entail changing bio's in order to determine the age from the opening sentence.
            
            - MANUALLY CHECK ALL the 'year only'-entries in the corresponding article.
            - Publish the changes in the actual month article
            - Add and update the references with this tool (menu item p).

         */

        public void Fix1995Phase1(int monthId)
        {
            string month = GetMonthNames().ElementAt(monthId - 1);
            string articleTitle = $"Deaths_in_{month}_1995"; // Oh nee..
            articleTitle = "User:Mill_1~~tmp";

            string uri = $"wikipedia/rawarticle/{articleTitle}/netto/false";
            HttpResponseMessage response = util.SendGetRequest(uri);

            string text = util.HandleResponse(response, articleTitle);

            text = TrimWikiText1995(text, month);
            text = text.Replace("* ", "*");
            text = text.Replace(" * ", " ");  // *[[Juanin Clay]], American actress and director (b. 1949)<ref>{{cite news|last1=Brady|first1=David E.|title=Obituaries : * Juanin Clay; Actress, Director
            text = text.Replace("\\\"", "\""); // WHY DOES THIS HAPPEN??

            text = RemoveEntriesWithoutArticle(text);
            text = RemoveCitationNeededTags(text);

            PrintTmpOutputPhase1(month, text);
        }

        private string RemoveEntriesWithoutArticle(string text)
        {           
            List<string> list = new List<string>();

            var chunks = text.Split("*");

            foreach (string chunk in chunks)
            {
                if (chunk.Substring(0, 2) == "[[")
                    list.Add("*" + chunk);
                else
                {
                    var pos = chunk.IndexOf("==");

                    if (pos != -1)
                        list.Add("\\n" + chunk.Substring(pos) ); // WHY ?!?
                }
            }
            return  string.Join(string.Empty, list);
        }

        private string RemoveCitationNeededTags(string text)
        {
            while (true)
            {
                var pos1 = text.IndexOf("{{citation needed");
                if (pos1 == -1)
                    break;

                var pos2 = text.IndexOf("}}", pos1 + 1);
                text = text.Substring(0, pos1) + text.Substring(pos2 + "}}".Length);
            }
            return text;
        }

        public void Fix1995Phase2(int monthId)
        {
            try
            {
                UI.Console.WriteLine("Hold on..");

                entries = GetEntriesPermonth(1995, monthId);
                CheckIfArticleContainsSublist(1995, monthId);
                PrintOutput(1995, monthId);

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

        private void PrintTmpOutputPhase1(string month, string text)
        {
            Directory.CreateDirectory("output1995");
            string fileName = $"Deaths in {month} 1995.txt";
            string file = Path.Combine("output1995", fileName);

            var lines = text.Split("\\n");

            using (var writer = File.CreateText(file))
            {
                foreach (var line in lines)
                {
                    writer.WriteLine(line);
                }
            }

            UI.Console.WriteLine($"Phase 1 complete. See folder:\r\n{Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "output1995")}");
        }

        // TODO verwijderen; gekopieerd van API, WikipediaService
        private string TrimWikiText1995(string wikiText, string month)
        {
            string trimmedText = wikiText;
            int pos;

            //Trim left
            pos = Math.Max(trimmedText.IndexOf($"=={month} 1995=="), trimmedText.IndexOf($"== {month} 1995 =="));

            if (pos == -1)
                throw new InvalidWikipediaPageException($"Not found:  ==[]{ month } 1995[]== ");

            trimmedText = trimmedText.Substring(pos);

            // Trim right
            pos = Math.Max(trimmedText.IndexOf("==References=="), trimmedText.IndexOf("== References =="));

            if (pos == -1)
                throw new InvalidWikipediaPageException($"Not found:  ==[]References[]== ");

            trimmedText = trimmedText.Substring(0, pos);

            return trimmedText;
        }


        public void PrintDeathsPerMonthArticle(int year, int monthId)
        {
            try
            {
                EvaluateDeathsPerMonthArticle(year, monthId);
                CheckIfArticleContainsSublist(year, monthId);

                UI.Console.WriteLine("\r\nEvaluation complete. Continue? (y/n)");

                if (UI.Console.ReadLine() != "y")
                    return;

                PrintOutput(year, monthId);

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

        private void CheckIfArticleContainsSublist(int year, int monthId)
        {
            string articleTitle = $"Deaths in {GetMonthNames().ElementAt(monthId - 1)} {year}";

            bool articleContainsSublist = articleAnalyzer.ArticleContainsSublist(articleTitle);

            if (articleContainsSublist)
                UI.Console.Write(ConsoleColor.Red, "\r\nATTENTION! Sublist(s) in article are not processed (yet)!");
        }

        private void EvaluateDeathsPerMonthArticle(int year, int monthId)
        {
            UI.Console.WriteLine("Getting things ready. This may take a minute..");

            entries = GetEntriesPermonth(year, monthId);
            references = GetReferencesPermonth(year, monthId);

            for (int day = 1; day <= DateTime.DaysInMonth(year, monthId); day++)
            {
                UI.Console.WriteLine($"\r\nChecking nyt ref. date {new DateTime(year, monthId, day).ToShortDateString()}");

                IEnumerable<Reference> referencesPerDay = references.Where(r => r.DeathDate.Day == day);

                foreach (var reference in referencesPerDay)
                    HandleReference(reference, entries);
            }
        }

        private void PrintOutput(int year, int monthId)
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
            Entry entry = entries.Where(e => e.LinkedName == reference.ArticleTitle).FirstOrDefault();

            if (entry == null)
            {
                int nettoNrOfChars = DetermineNumberOfCharactersBiography(reference);

                if (nettoNrOfChars >= minimumNrOfNettoCharsBiography)
                    UI.Console.WriteLine(ConsoleColor.Magenta, $"{reference.ArticleTitle} not in day subsection. (net # of chars bio: {nettoNrOfChars})");
            }
            else
                HandleExistingEntry(entry, reference);
        }

        private int DetermineNumberOfCharactersBiography(Reference reference)
        {
            int nettoNrOfChars = 0;

            try
            {
                // An entry could've be left out of the list because of notabiltiy. Determine netto nr of chars article
                nettoNrOfChars = GetNumberOfCharactersBiography(reference.ArticleTitle, netto: true);
            }
            catch (WikipediaReferencesException e)
            {
                UI.Console.WriteLine(ConsoleColor.Blue, e.Message);
            }
            catch (Exception)
            {
                throw;
            }

            return nettoNrOfChars;
        }

        private void HandleExistingEntry(Entry entry, Reference reference)
        {
            if (entry.DeathDate == reference.DeathDate)
            {
                ConsoleColor consoleColor = ConsoleColor.White;

                string refInfo = HandleMatchingDatesOfDeath(entry, reference, out consoleColor);

                UI.Console.WriteLine(consoleColor, $"{entry.LinkedName}: {refInfo}");
            }
            else
                PrintMismatchDateOfDeaths(reference, entry);
        }

        private string HandleMatchingDatesOfDeath(Entry entry, Reference reference,  out ConsoleColor consoleColor)
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
                    if (IsDurableSource(entry.Reference))
                    {
                        consoleColor = ConsoleColor.DarkGray;
                        return "Has durable source.";
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

        private bool IsDurableSource(string reference)
        {
            if (reference.Contains("latimes.com/") ||
                    reference.Contains("independent.co.uk/") ||
                    reference.Contains("news.bbc.co.uk/") ||
                    reference.Contains("telegraph.co.uk/") ||
                    reference.Contains("washingtonpost.com/") ||
                    reference.Contains("rollingstone.com/") ||
                    reference.Contains("economist.com/") ||
                    reference.Contains("irishtimes.com/") ||
                    reference.Contains("theguardian.com/"))
                return true;


            // <ref>[http..
            if (!reference.Contains("{{cite", StringComparison.OrdinalIgnoreCase) && !reference.Contains("{{citation", StringComparison.OrdinalIgnoreCase))
                return false;

            if (reference.Contains("{{cite news", StringComparison.OrdinalIgnoreCase) || reference.Contains("{{citation", StringComparison.OrdinalIgnoreCase))
                return false;
            else
                // books and journals are preferable over news sources
                return true;
        }

        private void PrintMismatchDateOfDeaths(Reference reference, Entry entry)
        {
            string message = $"Death date entry: {entry.DeathDate.ToShortDateString()} Url:\r\n{reference.Url}";

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
            // page redirects have been handled
            string uri = $"wikipedia/rawarticle/{articleTitle}/netto/{netto}";
            HttpResponseMessage response = util.SendGetRequest(uri);

            string rawArticleText = util.HandleResponse(response, articleTitle);

            return rawArticleText.Length;
        }

        private IEnumerable<Entry> GetEntriesPermonth(int year, int monthId)
        {
            IEnumerable<Entry> entries;            
            string uri = $"wikipedia/deceased/{year}/{monthId}";
            HttpResponseMessage response = util.SendGetRequest(uri);

            string result = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
                entries = JsonConvert.DeserializeObject<IEnumerable<Entry>>(result);
            else
            {
                HandleResultException(result);
                return null;
            }

            return entries;
        }

        private void HandleResultException(string result)
        {
            if (result.Contains(typeof(WikipediaPageNotFoundException).Name))
                throw new WikipediaReferencesException($"Redlink entry in the deaths per month article. Remove it.");
            else
            {
                if (result.Contains(typeof(InvalidWikipediaPageException).Name))
                    throw new WikipediaReferencesException(result);
                else
                    throw new Exception(result);
            }
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
