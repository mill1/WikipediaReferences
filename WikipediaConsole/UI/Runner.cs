
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using WikipediaReferences;
using WikipediaReferences.Models;

namespace WikipediaConsole.UI
{    
    public class Runner
    {
        private const string WikiListCheck = "w";
        private const string Test = "t";
        private const string AddNYTObitRefs = "a";
        private const string Quit = "q";
        private bool quit;

        private readonly IConfiguration configuration;
        private readonly HttpClient client;

        public Runner(IConfiguration configuration, HttpClient client, AssemblyInfo assemblyInfo)
        {
            this.configuration = configuration;

            this.client = client;
            var uri = configuration.GetValue<string>("WRWebApi:SchemeAndHost");
            this.client.BaseAddress = new Uri(uri);
            this.client.DefaultRequestHeaders.Accept.Clear();
            this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            quit = false;

            var assemblyName = assemblyInfo.GetAssemblyName();
            string appVersion = assemblyInfo.GetAssemblyValue("Version", assemblyName);

            Console.DisplayAssemblyInfo(assemblyName.Name, appVersion);
        }

        public void Run()
        {
            try
            {
                while (!quit)
                {
                    Console.DisplayMenu(ConsoleColor.Yellow, GetMenuItems());

                    string answer = Console.ReadLine();

                    ProcessAnswer(answer);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(ConsoleColor.Red, e);
                Console.ReadLine();            
            }
        }

        private List<string> GetMenuItems()
        {
            return new List<string>
            {
                $"{WikiListCheck}:\tWiki list: check NYT references",
                $"{Test}:\tTest stuff",
                $"{AddNYTObitRefs}:\tAdd NYT obituaries to db",
                $"{Quit}:\tQuit application"
            };
        }

        private void ProcessAnswer(string answer)
        {
            switch (answer)
            {
                case WikiListCheck:
                    CheckListArticle();
                    break;
                case Test:
                    TestGetDeceasedFromWikipedia();
                    break;
                case AddNYTObitRefs:
                    AddNYTimesObituaryReferences();
                    break;
                case Quit:
                    quit = true;
                    break;
                default:
                    Console.WriteLine($"Invalid choice: {answer}");
                    break;
            }
        }

        private void CheckListArticle()
        {
            try
            {
                // voorbeeld: 2 maart 1997
                int year = 1997;
                int monthId = 3;

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
                            // an entry could deliberately 've be left out of the list -> show netto nr of chars article?
                            int nettoNrOfChars = GetNumberOfCharactersBiography(reference.ArticleTitle, netto: true);
                            Console.WriteLine(ConsoleColor.Magenta, $"{reference.ArticleTitle} not in day subsection. (net # of chars bio: {nettoNrOfChars})");
                        }                            
                        else
                        {
                            if (entry.DeathDate == reference.DeathDate)
                                Console.WriteLine(ConsoleColor.Green, entry.LinkedName);
                            else
                                Console.WriteLine(ConsoleColor.Red, $"{entry.LinkedName} death date entry: {entry.DeathDate.ToShortDateString()}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(ConsoleColor.Red, e);
            }
        }

        private int GetNumberOfCharactersBiography(string articleTitle, bool netto)
        {
            // page redirects have been handled
            HttpResponseMessage response;

            string uri = $"wikipedia/articleraw/{articleTitle}";
            string result = SendGetRequest(uri, out response);

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
            string result = SendGetRequest(uri, out response);

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

            string result = SendGetRequest(uri, out response);

            if (response.IsSuccessStatusCode)
                references = JsonConvert.DeserializeObject<IEnumerable<Reference>>(result);
            else
                throw new Exception(result);

            return references;
        }

        private void TestGetDeceasedFromWikipedia()
        {
            string uri = $"wikipedia/deceased/1997/3";

            HttpResponseMessage response;
            string result = SendGetRequest(uri, out response);
            IEnumerable<Entry> entries = JsonConvert.DeserializeObject<IEnumerable<Entry>>(result);

            var refs = entries.Where(e => e.Reference != null);
            
            int maxLength = entries.Max(e => e.Information.Length);
            Entry entry = entries.Where(e => e.Information.Length == maxLength).First();

            Console.WriteLine($"Nr of entries: {entries.Count()}");
            Console.WriteLine($"Nr of entries with references: {refs.Count()}");
            Console.WriteLine($"Longest entry (excl. ref):  {entry.Name}");
            Console.WriteLine($"Longest entry value:\r\n{entry}");
        }

        private void AddNYTimesObituaryReferences()
        {
            try
            {
                string uri = GetAddObitsApiUri();

                HttpResponseMessage response;
                string result = SendGetRequest(uri, out response);

                if (response.IsSuccessStatusCode)
                    Console.WriteLine(ConsoleColor.Green, result);
                else
                    throw new ArgumentException(result);

            }
            catch (ArgumentException e)
            {
                Console.WriteLine(ConsoleColor.Magenta, e);
            }
            catch (Exception e)
            {
                Console.WriteLine(ConsoleColor.Red, e);
            }
        }

        private string SendGetRequest(string uri, out HttpResponseMessage response)
        {
            Console.WriteLine("Processing request. Please wait...\r\n");
            response = client.GetAsync(uri).Result;

            return response.Content.ReadAsStringAsync().Result;
        }

        private string GetAddObitsApiUri()
        {
            const string ApiKey = "NYTimes Archive API key";

            Console.WriteLine("Death year:");
            int year = int.Parse(Console.ReadLine());
            Console.WriteLine("Death month id: (March = 3)");
            int monthId = int.Parse(Console.ReadLine());
            string apiKey = configuration.GetValue<string>(ApiKey);

            if (apiKey == null || apiKey == "TOSET")
            {
                Console.WriteLine(ApiKey + ":");
                apiKey = Console.ReadLine();
            }

            return $"nytimes/addobits/{year}/{monthId}/{apiKey}";
        }
    }
}
