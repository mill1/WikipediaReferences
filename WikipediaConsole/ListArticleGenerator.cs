using System;
using System.Collections.Generic;
using System.Net.Http;
using WikipediaReferences;
using System.Text;
using WikipediaReferences.Models;
using System.Linq;
using Newtonsoft.Json;

namespace WikipediaConsole
{
    public class ListArticleGenerator
    {
        private const int MinimumNrOfNettoCharsBiography = 2000;

        private readonly Util util;

        public ListArticleGenerator(Util util)
        {
            this.util = util;
        }

        public void InspectListArticle()
        {
            try
            {
                // TODO obviously
                Console.WriteLine("Death year:");
                int year = int.Parse(Console.ReadLine());
                Console.WriteLine("Death month id: (March = 3)");
                int monthId = int.Parse(Console.ReadLine());

                Console.WriteLine("Getting things ready. This may take a minute..");
                IEnumerable<Entry> entries;
                entries = GetEntriesPermonth(year, monthId);

                IEnumerable<Reference> references;
                references = GetReferencesPermonth(year, monthId);

                for (int day = 1; day <= DateTime.DaysInMonth(year, monthId); day++)
                {
                    IEnumerable<Reference> referencesPerDay = references.Where(r => r.DeathDate.Day == day);

                    Console.WriteLine($"\r\nInspecting date {new DateTime(year, monthId, day).ToShortDateString()}");

                    foreach (var reference in referencesPerDay)
                    {
                        //Get matching entry 
                        Entry entry = entries.Where(e => e.LinkedName == reference.ArticleTitle).FirstOrDefault();

                        if (entry == null)
                        {
                            // an entry could've be left out of the list because of notabilty. Determine netto nr of chars article
                            int nettoNrOfChars = GetNumberOfCharactersBiography(reference.ArticleTitle, netto: true);

                            if (nettoNrOfChars >= MinimumNrOfNettoCharsBiography)
                                UI.Console.WriteLine(ConsoleColor.Magenta, $"{reference.ArticleTitle} not in day subsection. (net # of chars bio: {nettoNrOfChars})");
                        }
                        else
                        {
                            if (entry.DeathDate == reference.DeathDate)
                            {
                                ConsoleColor consoleColor = ConsoleColor.Green;
                                string refInfo;

                                if (entry.Reference == null)
                                {
                                    // Entry found for which a NYT obit. exists. Set access date of reference to add to today.
                                    reference.AccessDate = DateTime.Today;
                                    refInfo = "New NYT reference!";
                                }
                                else
                                {
                                    if (entry.Reference.Contains("New York Times") && !entry.Reference.Contains("paid notice", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // Entry has NYT obituary reference. Update the reference but keep the access date of the original ref.
                                        reference.AccessDate = GetAccessDateFromEntryReference(entry.Reference, reference.AccessDate);
                                        refInfo = $"Update NYT reference! Access date = {reference.AccessDate.ToShortDateString()}";
                                    }
                                    else
                                    {
                                        consoleColor = ConsoleColor.DarkGreen;
                                        refInfo = "Non-NYT reference";
                                    }
                                }
                                UI.Console.WriteLine(consoleColor, $"{entry.LinkedName}: {refInfo}");
                            }
                            else
                            {
                                string message = $"Death date entry: {entry.DeathDate.ToShortDateString()} Url:\r\n{reference.Url}";

                                if (entry.Reference == null)
                                    UI.Console.WriteLine(ConsoleColor.Red, $"{entry.LinkedName}: New NYT reference! {message}");
                                else
                                    if (entry.Reference.Contains("New York Times"))
                                    UI.Console.WriteLine(ConsoleColor.Red, $"{entry.LinkedName}: Update NYT reference! {message}");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                UI.Console.WriteLine(ConsoleColor.Red, e);
            }
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
                int posEnd = Math.Min(entryReference.IndexOf("|", posStart), entryReference.IndexOf("}}", posStart));

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
            HttpResponseMessage response;

            string uri = $"wikipedia/rawarticle/{articleTitle}/netto/true";
            string result = util.SendGetRequest(uri, out response);

            if (response.IsSuccessStatusCode)
                return result.Length;
            else
                throw new Exception(result);
        }

        private IEnumerable<Entry> GetEntriesPermonth(int year, int monthId)
        {
            IEnumerable<Entry> entries;
            HttpResponseMessage response;

            string uri = $"wikipedia/deceased/{year}/{monthId}";
            string result = util.SendGetRequest(uri, out response);

            if (response.IsSuccessStatusCode)
                entries = JsonConvert.DeserializeObject<IEnumerable<Entry>>(result);
            else
                throw new Exception(result);

            return entries;
        }

        private IEnumerable<Reference> GetReferencesPermonth(int year, int monthId)
        {
            IEnumerable<Reference> references;
            HttpResponseMessage response;

            string uri = $"nytimes/reference/{year}/{ monthId}";

            string result = util.SendGetRequest(uri, out response);

            if (response.IsSuccessStatusCode)
                references = JsonConvert.DeserializeObject<IEnumerable<Reference>>(result);
            else
                throw new Exception(result);

            return references;
        }

    }
}
