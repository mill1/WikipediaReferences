﻿
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using WikipediaReferences;

namespace WikipediaConsole.UI
{    
    public class Runner
    {       
        private const string WikiListCheck = "w";
        private const string DayCheck = "d";
        private const string Test = "t";
        private const string AddNYTObitRefs = "a";
        private const string Quit = "q";
        private bool quit;

        private readonly IConfiguration configuration;
        private readonly Util util;
        private readonly ListArticleGenerator listArticleGenerator;

        public Runner(IConfiguration configuration, Util util, ListArticleGenerator listArticleGenerator, AssemblyInfo assemblyInfo)
        {
            this.configuration = configuration;
            this.util = util;
            this.listArticleGenerator = listArticleGenerator;

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
                $"{DayCheck}:\tDay name of date",
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
                    listArticleGenerator.InspectListArticle();
                    break;
                case DayCheck:
                    GetDaynameFromDate();
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

        private void GetDaynameFromDate()
        {
            Console.WriteLine("Date: (yyyy-M-d)");
            DateTime date = DateTime.Parse(Console.ReadLine());

            Console.WriteLine(ConsoleColor.Blue, date.DayOfWeek.ToString());
        }

        private void TestGetDeceasedFromWikipedia()
        {
            string uri = $"wikipedia/deceased/1997/3";

            HttpResponseMessage response;
            string result = util.SendGetRequest(uri, out response);
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
                string result = util.SendGetRequest(uri, out response);

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
