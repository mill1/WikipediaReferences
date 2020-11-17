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

namespace WikipediaConsole.Services
{
    public class ListArticleGenerator
    {
        private const int MinimumNrOfNettoCharsBiography = 5000;

        private readonly Util util;

        private IEnumerable<Entry> entries;
        private  IEnumerable<Reference> references;

        public ListArticleGenerator(Util util)
        {
            this.util = util;
        }

        public void PrintDeathsPerMonthArticle(int year, int monthId)
        {
            EvaluateDeathsPerMonthArticle(year, monthId);

            UI.Console.WriteLine("\r\nEvaluation complete. Continue? (y/n)");
            if (UI.Console.ReadLine() != "y")
                return;

            PrintOutput(year, monthId);

            UI.Console.WriteLine($"List generated. See folder:\r\n{Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "output")}");
        }

        private void EvaluateDeathsPerMonthArticle(int year, int monthId)
        {
            try
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
            catch (WikipediaReferencesException e)
            {
                UI.Console.WriteLine(ConsoleColor.Magenta, e.Message);
            }
            catch (Exception e)
            {
                UI.Console.WriteLine(ConsoleColor.Red, e);
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
                writer.WriteLine($"=={GetMonthNames().ElementAt(monthId - 1)} {year}==");

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

                if (nettoNrOfChars >= MinimumNrOfNettoCharsBiography)
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
                // An entry could've be left out of the list because of notabilty. Determine netto nr of chars article
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

                return "New NYT reference!";
            }
            else
            {
                if (entry.Reference.Contains("New York Times") && !entry.Reference.Contains("paid notice", StringComparison.OrdinalIgnoreCase))
                {
                    // Entry has NYT obituary reference. Update the reference but keep the access date of the original ref.
                    reference.AccessDate = GetAccessDateFromEntryReference(entry.Reference, reference.AccessDate);
                    entry.Reference = reference.GetNewsReference();
                    consoleColor = ConsoleColor.Green;

                    return $"Updateable NYT reference! Access date = {reference.AccessDate.ToShortDateString()}";
                }
                else
                {
                    consoleColor = ConsoleColor.DarkGreen;
                    return "Non-NYT reference";
                }
            }
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
                if (result.Contains(typeof(WikipediaPageNotFoundException).Name))
                    throw new WikipediaReferencesException($"Redlink entry in the deaths per month article. Remove it.");
                else
                    throw new Exception(result);
            }
            
            return entries;
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
