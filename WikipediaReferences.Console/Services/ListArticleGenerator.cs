using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Wikimedia.Utilities.Exceptions;
using Wikimedia.Utilities.ExtensionMethods;
using WikipediaReferences.Models;

namespace WikipediaReferences.Console.Services
{
    public class ListArticleGenerator
    {
        private readonly IConfiguration configuration;
        private readonly Util util;
        private readonly ArticleAnalyzer articleAnalyzer;
        private readonly HttpClient httpClient;

        public ListArticleGenerator(IConfiguration configuration, Util util, ArticleAnalyzer articleAnalyzer)
        {
            this.configuration = configuration;
            this.util = util;
            this.articleAnalyzer = articleAnalyzer;
            httpClient = new HttpClient();
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

        public void GenerateStatisticsPerYear(int y)
        {
            string fileName = $"Sublists 1990-2005.txt";
            string file = Path.Combine("output", fileName);
            using (var writer = File.AppendText(file))
            {
                for (int year = 1990; year <= 2005; year++)
                {
                    for (int monthId = 1; monthId <= 12; monthId++)
                    {
                        string monthName = GetMonthNames().ElementAt(monthId - 1);
                        UI.Console.WriteLine(ConsoleColor.Blue, $"Processing {monthName} {year}...");

                        var articleTitle = $"Deaths in {monthName} {year}";

                        if (articleAnalyzer.ArticleContainsSublist(articleTitle))
                        {
                            UI.Console.WriteLine(ConsoleColor.DarkGreen, "Sublist found!");
                            writer.WriteLine(articleTitle);
                        }
                    }
                }
            }

            UI.Console.WriteLine($"Pages with sublists in 1990-2005 generated. See folder:\r\n{Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "output")}");
        }

        public void GenerateStatisticsPerYearOrig(int y)
        {
            string fileName = $"Entries 1990-2005.txt";
            string file = Path.Combine("output", fileName);
            using (var writer = File.AppendText(file))
            {
                writer.Write($"Year;MonthId;Day;Name;Age;Nationality;Occupation;CoD;InformationPart\r\n");

                for (int year = 1990; year <= 2005; year++)
                {
                    for (int monthId = 1; monthId <= 12; monthId++)
                    {
                        string monthName = GetMonthNames().ElementAt(monthId - 1);
                        UI.Console.WriteLine(ConsoleColor.Blue, $"Processing {monthName} {year}...");
                        GenerateStatisticsPerMonth(year, monthId, writer);
                    }
                }
            }

            UI.Console.WriteLine($"Entries for 1990-2005 generated. See folder:\r\n{Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "output")}");
        }        

        private void GenerateStatisticsPerMonth(int year, int monthId, StreamWriter writer)
        {
            try
            {
                string monthName = GetMonthNames().ElementAt(monthId - 1);
                string articleTitle = $"Deaths in {monthName} {year}";

                long revisionId = GetRevisionID(articleTitle, new DateTime(2024, 11, 7));
                
                articleTitle += $"&oldid={revisionId}";

                UI.Console.WriteLine($"Fetching the entries from article {articleTitle}...");
                var entries = GetEntriesPermonth(year, monthId, articleTitle);

                foreach (var entry in entries)
                {                        
                    var entryDetails = ResolveEntryDetails(entry);
                    writer.Write($"{entryDetails}\r\n");
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

        public EntryDetails ResolveEntryDetails(Entry entry)
        {
            switch (entry.Name)
            {
                case "Pete Abele":
                    entry.Information = "83, American politician ([[United States House of Representatives|U.S. Representative]] for [[Ohio's 10th congressional district]]), [[Alzheimer's disease]]";
                    break;
                case "Howard Earl Johnston":
                    entry.Information = "72, Canadian member of [[Parliament of Canada|Parliament]] ([[House of Commons of Canada|House of Commons]])";
                    break;
                case "Eudora Welty":
                    entry.Information = "92, American short story writer and [[novelist]] ([[Pulitzer Prize]] for ''[[The Optimist's Daughter]])'', [[pneumonia]]";
                    break;
                case "Donald Hume":
                    entry.Information = "86, American Olympic [[Rowing (sport)|rower]] ([[Rowing at the 1936 Summer Olympics|gold medal winner]] in [[Rowing at the 1936 Summer Olympics – Men's eight|men's rowing eight]] at the [[1936 Summer Olympics]]).";
                    break;
                case "Christabel Bielenberg":
                    entry.Information = "94, British writer (''The Past is Myself'', ''[[Christabel (1988 TV drama)|Christabel]]'').";
                    break;
                case "Arthur Miller":
                    entry.Information = "89, American playwright (''[[Death of a Salesman]]'', ''[[A View from the Bridge]]'', ''[[The Crucible]]''), congestive heart failure.";
                    break;
                case "Manuel Mejía Vallejo":
                    entry.Information = "75, Colombian writer.";
                    break;
                case "Roberto Matta":
                    entry.Information = "91, Chilean artist.";
                    break;
                case "Kell Areskoug":
                    entry.Information = "90, Swedish Olympic sprinter.";
                    break;
                case "Marie Jahoda":
                    entry.Information = "94, Austrian-British [[social psychology|social psychologist]].";
                    break;
                case "Harald Bergström":
                    entry.Information = "93, Swedish [[mathematician]].";
                    break;
                case "Gérard Séty":
                    entry.Information = "75, French actor.";
                    break;
                case "Paul Baize":
                    entry.Information = "94, French pediatrician and amateur astronomer.";
                    break;
                case "Agenore Incrocci":
                    entry.Information = "86, Italian screenwriter, heart attack.";
                    break;
                case "Mary Bunting":
                    entry.Information = "87, American academic and college president.";
                    break;
                case "Bodacious":
                    entry.Information = "11–12, American [[bucking bull]] and [[ProRodeo Hall of Fame]] inductee.";
                    break;
                case "Cassie Bernall":
                    entry.Information = "17, American school shooting victim, shot.";
                    break;
                case "Eric Harris":
                    entry.Information = "18, American school shooting perpetrator, suicide by gunshot.";
                    break;
                case "Dylan Klebold":
                    entry.Information = "17, American school shooting perpetrator, suicide by gunshot.";
                    break;
                case "Rachel Scott":
                    entry.Information = "17, American school shooting victim, shot.";
                    break;
                case "Ameer Hamza Shinwari":
                    entry.Information = "87, Afghan [[Pashto]]-language poet.";
                    break;
                case "Herbert Heilpern":
                    entry.Information = "80, Austrian-American [[association football]] executive.";
                    break;
                case "Danielle de St. Jorre":
                    entry.Information = "55, Seychellois politician, cancer.";
                    break;
                case "Sopubek Begaliev":
                    entry.Information = "71, Soviet-Kyrgyz economist and politician.";
                    break;
                case "Babatunde Olatunji":
                    entry.Information = "75, Nigerian drummer, recorded ''Drums of Passion'', [[diabetes]].";
                    break;
                case "Ken Meyr":
                    entry.Information = "71, American-Australian businessman, diplomat, and philanthropist.";
                    break;
                case "Susi Jeans":
                    entry.Information = "81, Austrian-British organist, teacher and musicologist.";
                    break;
                case "Ranasinghe Premadasa":
                    entry.Information = "68, [[President of Sri Lanka]] (1989-1993), assassination by suicide bomber.";
                    break;
                case "Mehrdad Bahar":
                    entry.Information = "64, Iranian [[Iranist]], linguist, mythologist and Persian historian.";
                    break;
                case "Marie Epstein":
                    entry.Information = "95, French actress, film director, and film preservationist.";
                    break;
                case "Frank Chacksfield":
                    entry.Information = "English pianist, arranger and orchestra leader, [[Parkinson's disease]].";
                    break;
                case "Country Dick Montana":
                    entry.Information = "40, American singer, [[aneurysm]].";
                    break;
                case "Yuri Neprintsev":
                    entry.Information = "87, Soviet/Russian painter, graphic artist and art teacher.";
                    break;
                case "Sándor Végh":
                    entry.Information = "84, Hungarian-French violinist and conductor.";
                    break;
                case "Amasa Stone Bishop":
                    entry.Information = "76, American nuclear physicist, [[pneumonia]].";
                    break;
                case "Elizabeth Bauer Mock":
                    entry.Information = "86–87, American professor, curator, author and journalist.";
                    break;
                case "Oleg Prokofiev":
                    entry.Information = "69, Soviet and Russian artist, sculptor and poet.";
                    break;
                case "Freaky Tah":
                    entry.Information = "27, American [[rapping|MC]], [[hype man]] and [[hip hop music|hip hop]] promoter, shot.";
                    break;
                case "Wynona Lipman":
                    entry.Information = "75–76, American politician, cancer.";
                    break;
                case "Thomas Ypsilantis":
                    entry.Information = "72, Greek-American physicist, heart attack.";
                    break;
                case "John Ardoin":
                    entry.Information = "66, American music critic and author.";
                    break;
                case "Hüseyin Hilmi Işık":
                    entry.Information = "90, Turkish [[Sunni Islamic]] scholar.";
                    break;
                case "Nicolas Ruwet":
                    entry.Information = "Belgian linguist, literary critic and musical analyst.";
                    break;
                case "Lor Tok":
                    entry.Information = "88, Thai comedian and actor.";
                    break;
                case "Dorle Soria":
                    entry.Information = "101, American publicist, music producer and journalist.";
                    break;
                case "Stan Rice":
                    entry.Information = "60, American painter, educator, poet, husband of author [[Anne Rice]], cancer.";
                    break;
                case "Nell Carter":
                    entry.Information = "54, American singer, actress (''[[Gimme a Break!]]'', ''[[Hangin' with Mr. Cooper]]'', ''[[The Grass Harp (film)|The Grass Harp]]''), [[diabetes]].";
                    break;
                case "Stacy Keach, Sr.":
                    entry.Information = "88, American actor (''[[Pretty Woman]]'', ''[[Teen Wolf (1986 TV series)|Teen Wolf]]'', ''[[The Parallax View]]'').";
                    break;
                case "Izhak Graziani":
                    entry.Information = "78, Israeli conductor.";
                    break;
                case "Kimal Akishev":
                    entry.Information = "79, Kazakhstani scientist, archeologist, and historian.";
                    break;
                case "Vittore Branca":
                    entry.Information = "90, Italian [[Philology|philologist]], [[Literary criticism|literary critic]], and academic.";
                    break;
                case "Asen Kisimov":
                    entry.Information = "69, Bulgarian actor, singer and radio presenter.";
                    break;
                case "Arman":
                    entry.Information = "76, French-American sculptor, cancer.";
                    break;
                case "name1":
                    entry.Information = "";
                    break;
            }

            string informationPart = entry.Information.Replace("  ", " ");
            informationPart = informationPart.Replace(", ", ",");
            informationPart = informationPart.Replace("-born ", "");
            informationPart = informationPart.Replace("U.S.", "American");
            informationPart = informationPart.Replace("US Army", "American Army");
            informationPart = informationPart.Replace("US Air", "American Air");
            informationPart = informationPart.Replace("US Marine", "American Marine");
            informationPart = informationPart.Replace("African-American", "American");
            informationPart = informationPart.Replace("African American", "American");
            informationPart = informationPart.Replace("Afrikaans", "South African");
            informationPart = informationPart.Replace("Aboriginal Australian", "Australian");
            informationPart = informationPart.Replace("Kosovar Albanian", "Albanian");
            informationPart = informationPart.Replace("Kosovo Albanian", "Albanian");
            informationPart = informationPart.Replace("Kosovo-Albanian", "Albanian");
            informationPart = informationPart.Replace("Kosovan", "Albanian");
            informationPart = informationPart.Replace("Soviet and ", "");
            informationPart = informationPart.Replace("Soviet/", "");
            informationPart = informationPart.Replace("Soviet-", "");
            informationPart = informationPart.Replace("Suriname ", "Surinamese ");
            informationPart = informationPart.Replace("Tajikistan", "Tajik");
            informationPart = informationPart.Replace("Tajikistani", "Tajik");
            informationPart = informationPart.Replace("Trinidad and Tobago", "Trinidadian");
            informationPart = informationPart.Replace("Trinidad and Tobagian", "Trinidadian");
            informationPart = informationPart.Replace("Trinidad", "Trinidadian");

            informationPart = informationPart.Replace("British Virgin Islan", "British-Virgin-Islands");
            informationPart = informationPart.Replace("British Virgin Islander", "British-Virgin-Islands");
            informationPart = informationPart.Replace("British Virgin Island", "British-Virgin-Islands");
            informationPart = informationPart.Replace("United States Virgin Islands", "United-States-Virgin-Islands");
            informationPart = informationPart.Replace("American Virgin Islander", "United-States-Virgin-Islands");
            informationPart = informationPart.Replace("Papua New Guinean", "Papua-New-Guinean");

            informationPart = informationPart.Replace("Central African Republic", "Central African");
            informationPart = informationPart.Replace("Salvadorian", "Salvadoran");
            informationPart = informationPart.Replace("El Salvador", "Salvadoran");

            informationPart = informationPart.Replace("New Zealand-", "");
            informationPart = informationPart.Replace("United States-", "");
            informationPart = informationPart.Replace("South African-", "");
            informationPart = informationPart.Replace("South Korean-", "");
            informationPart = informationPart.Replace("North Korean-", "");
            informationPart = informationPart.Replace("Hong Kong-", "");
            informationPart = informationPart.Replace("Costa Rican-", "");
            informationPart = informationPart.Replace("Puerto Rican-", "");
            informationPart = informationPart.Replace("Saudi Arabian-", "");
            informationPart = informationPart.Replace("Sri Lankan-", "");

            informationPart = informationPart.Replace("New Zealand/", "");
            informationPart = informationPart.Replace("United States/", "");
            informationPart = informationPart.Replace("South African/", "");
            informationPart = informationPart.Replace("South Korean/", "");
            informationPart = informationPart.Replace("North Korean/", "");
            informationPart = informationPart.Replace("Hong Kong/", "");
            informationPart = informationPart.Replace("Costa Rican/", "");
            informationPart = informationPart.Replace("Puerto Rican/", "");
            informationPart = informationPart.Replace("Saudi Arabian/", "");
            informationPart = informationPart.Replace("Sri Lankan/", "");

            int pos = informationPart.IndexOf("<ref");  // named refs
            if(pos != -1)
                informationPart = informationPart.Substring(0, pos);

            // Deze volgorde vanwege geneste '(':   76, American film editor (''[[Cabaret (1972 film)|Cabaret]]'', ..
            informationPart = RemoveWikiLinks(informationPart);
            informationPart = RemoveParentheses(informationPart);

            if (informationPart.StartsWith(",")) // ",84,Argentine football player."
                informationPart = informationPart.Substring(1);

            informationPart = informationPart.Replace("  ", " ");
            informationPart = informationPart.Replace(" .", ".");

            Debug.WriteLine(informationPart);

            switch (entry.Name)
            {
                case "Joey Ramone":
                    informationPart = "49,American musician,lead singer for The Ramones,lymphoma.";
                    break;
                case "Rishikesh Shah":
                    informationPart = "77,Nepalese writer,politician and human rights activist.";
                    break;
                case "Yoram Ben-Porat":
                    informationPart = "55,Israeli academic and economist,traffic collision.";
                    break;
                case "Mehmed Orhan":
                    informationPart = "86,Ottoman prince and head of the Ottoman dynasty.";
                    break;
                case "Sooranad Kunjan Pillai":
                    informationPart = "83, Indian writer, literary critic, and scholar of the Malayalam language.";
                    break;
                case "Dr.Rajkumar":
                    informationPart = "80,Indian film actor.";
                    break;
                case "Pete Abele":
                    informationPart = "83,American politician,Alzheimer's disease.";
                    break;
            }

            List<string> infoParts = informationPart.Split(",", StringSplitOptions.None).ToList();

            if (infoParts.Count == 1)
            {
                infoParts = new List<string>
                {
                    "?",         // age
                    infoParts[0] // the rest
                };
            }

            var age = infoParts[0];

            var notabilityParts = infoParts[1].Split(" ", StringSplitOptions.None).ToList();


            string nationality;

            if (notabilityParts.Count == 1)
            {
                nationality = notabilityParts[0]; // Very rare
            }
            else
            {
                // Dual nationality: 2nd one
                if (notabilityParts[0].Contains("-") || notabilityParts[0].Contains("/"))
                {
                    int pos1 = notabilityParts[0].IndexOf("-");
                    int pos2 = notabilityParts[0].IndexOf("/");

                    notabilityParts[0] = notabilityParts[0].Substring(Math.Max(pos1, pos2) + 1);
                }

                switch ($"{notabilityParts[0]} {notabilityParts[1]}")
                {
                    case "Saint Lucian":
                        nationality = "Saint Lucian";
                        break;
                    case "Saint Vincentian":
                        nationality = "Saint Vincentian";
                        break;
                    case "Saint Kitts":
                        nationality = "Saint Kitts";
                        break;
                    case "Solomon Islands":
                        nationality = "Solomon Islands";
                        break;
                    case "New Zealand":
                        nationality = "New Zealand";
                        break;
                    case "New Caledonian":
                        nationality = "New Caledonian";
                        break;
                    case "United States":
                        nationality = "United States";
                        break;
                    case "South African":
                        nationality = "South African";
                        break;
                    case "South Korean":
                        nationality = "South Korean";
                        break;
                    case "South Ossetian":
                        nationality = "South Ossetian";
                        break;
                    case "South Vietnamese":
                        nationality = "South Vietnamese";
                        break;
                    case "North Korean":
                        nationality = "North Korean";
                        break;
                    case "Northern Irish":
                        nationality = "Northern Irish";
                        break;
                    case "West German":
                        nationality = "West German";
                        break;
                    case "West Indian":
                        nationality = "West Indian";
                        break;
                    case "East German":
                        nationality = "East German";
                        break;
                    case "East Timorese":
                        nationality = "East Timorese";
                        break;
                    case "Hong Kong":
                        nationality = "";
                        break;
                    case "Costa Rican":
                        nationality = "Costa Rican";
                        break;
                    case "Central African":
                        nationality = "Central African";
                        break;
                    case "Prime Minister":
                        nationality = "Prime Minister";
                        break;
                    case "Puerto Rican":
                        nationality = "Puerto Rican";
                        break;
                    case "Saudi Arabian":
                        nationality = "Saudi Arabian";
                        break;
                    case "Sierra Leonean":
                        nationality = "Sierra Leonean";
                        break;
                    case "Sri Lankan":
                        nationality = "Sri Lankan";
                        break;
                    default:
                        nationality = notabilityParts[0];
                        break;                        
                }
            }                            

            // notabilityInfo = informationPart minus age and nationality
            string notabilityInfo = informationPart.Replace($"{age},{nationality} ", "");

            var cod = infoParts[infoParts.Count - 1];
            if (cod.Last() == '.')
                cod = cod.Substring(0, cod.Count() - 1);

            if (cod == string.Empty)
            {
                // All fixed. System.Console.WriteLine($"#########\r\n######### {entry.Name}: CoD is empty; fix it\r\n#########");
                cod = " ";
            }

            return new EntryDetails
            {
                DeathDate = entry.DeathDate,
                Name = entry.Name.Replace(",", ""),
                Age = age,
                Nationality = nationality,
                Occupation = ResolveOccupation(notabilityInfo),
                CauseOfDeath = cod.CapitalizeFirstLetter(),
                InformationPart = informationPart
            };
        }

        private string ResolveOccupation(string notabilityInfo)
        {

            int pos1 = notabilityInfo.IndexOf(" and ");
            int pos2 = notabilityInfo.IndexOf(",");
            
            string occupation;

            if (pos1 == -1 && pos2 == -1)
            {
                occupation =  notabilityInfo.Replace(".", "");
            }
            else if (pos1 != -1 && pos2 != -1)
            {
                occupation =  notabilityInfo.Substring(0, Math.Min(pos1, pos2));
            }
            else
            {
                occupation =  notabilityInfo.Substring(0, Math.Max(pos1, pos2));
            }

            int pos = occupation.IndexOf("(");

            if(pos >= 0)
            {
                occupation = occupation.Substring(0, pos);
            }

            return occupation.Trim().CapitalizeFirstLetter();
        }

        private string RemoveWikiLinks(string informationPart)
        {

            while (informationPart.Contains("[["))
            {
                informationPart = RemoveFirstWikiLink(informationPart);
            }

            return informationPart;
        }

        private string RemoveFirstWikiLink(string informationPart)
        {
            if (!informationPart.Contains("[["))
                return informationPart;

            string text = informationPart.Substring("[[".Length, informationPart.IndexOf("]]") - "]]".Length);
            int posStart = informationPart.IndexOf("[[");
            

            int posEnd = informationPart.IndexOf("]]");
            int posPipe = informationPart.IndexOf("|");

            if(posPipe == -1 || posPipe > posEnd)  // f.i. "... [[My name]] the [[Link|Show]]"
            {
                text = informationPart.Substring(0, posStart);
                text += informationPart.Substring(posStart + 2, posEnd - posStart - 2);
                text += informationPart.Substring(posEnd + 2);
                return text;
            }

            if (posPipe < posStart)
                throw new WikipediaReferencesException($"Pipe before '[['?? Info part: {informationPart}");

            text = informationPart.Substring(0, posStart);
            text += informationPart.Substring(posPipe + 1, posEnd - posPipe - 1);
            text += informationPart.Substring(posEnd + 2);

            return text;
        }

        private string RemoveParentheses(string informationPart)
        {
            var text = informationPart;

            int i  = 0;

            while (text.Contains("("))
            {
                i++;
                text = RemoveFirstParenthesesPair(text);

                if (i > 5)
                    throw new WikipediaReferencesException($"Paranthese check. InformationPart = {informationPart}");
            }            
            return text;
        }

        private string RemoveFirstParenthesesPair(string informationPart)
        {
            if (!informationPart.Contains("("))
                return informationPart;

            int posStart = informationPart.IndexOf("(");
            int posEnd = informationPart.IndexOf(")");

            if (posEnd == -1)
                throw new WikipediaReferencesException("No matching ')' found!");

            string text = informationPart.Substring(0, posStart);          
            text += informationPart.Substring(posEnd + 1);

            return text;
        }

        private long GetRevisionID(string article, DateTime revisionDate)
        {
            // https://stackoverflow.com/questions/27376133/c-httpclient-with-post-parameters

            var parameters = new Dictionary<string, string>
            {
                { "action", "query" },
                { "format", "json" },
                { "prop", "revisions" },
                { "titles", article },
                { "rvprop", "ids" },
                { "rvlimit", "1" },
                { "rvstart", $"{revisionDate:yyyy-MM-dd}T00:00:00.000Z" }
            };

            HttpResponseMessage response = PostRequest(parameters);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var contents = response.Content.ReadAsStringAsync().Result;
                JObject json = JObject.Parse(contents);
                var x = json["query"]["pages"].First().First()["revisions"].First()["revid"];

                return (long)((JValue)x).Value;
            }

            throw new ArgumentException("Something went wrong (GetRevisionID)");
        }

        private HttpResponseMessage PostRequest(Dictionary<string, string> parameters)
        {
            string url = "https://en.wikipedia.org/w/api.php";

            var encodedContent = new FormUrlEncodedContent(parameters);
            var response = httpClient.PostAsync(url, encodedContent).Result;
            return response;
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
                var directLinks = 24;
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
