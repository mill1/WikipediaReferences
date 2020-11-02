
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using WikipediaReferences.Interfaces;

namespace WikipediaConsole.UI
{    
    public class Runner
    {
        private const string Test = "t";
        private const string AddNYTObitRefs = "a";
        private const string Quit = "q";
        private bool quit;

        private readonly IConfiguration configuration;
        private readonly HttpClient client;
        private readonly IWikipediaService wikipediaService;

        public Runner(IConfiguration configuration, HttpClient client, IWikipediaService wikipediaService, AssemblyInfo assemblyInfo)
        {
            this.configuration = configuration;
            this.wikipediaService = wikipediaService;

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
                $"{Test}:\tTest stuff",
                $"{AddNYTObitRefs}:\tAdd NYT obituaries to db",
                $"{Quit}:\tQuit application"
            };
        }

        private void ProcessAnswer(string answer)
        {
            switch (answer)
            {
                case Test:
                    //TestGetDeceasedPerDay();
                    TestConfigSetting();
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

        private async void AddNYTimesObituaryReferences()
        {
            try
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

                //string uri = $"nytimes/addobits/{year}/{monthId}/{apiKey}";

                //Console.WriteLine("Processing request. Please wait...\r\n");
                //HttpResponseMessage response = await client.GetAsync(uri);

                //string message = await response.Content.ReadAsStringAsync();

                //if (response.IsSuccessStatusCode)
                //    Console.WriteLine(ConsoleColor.Green, message);
                //else
                //    throw new ArgumentException(message);

                for (int m = 1; m <= 12; m++) 
                {
                    string uri = $"nytimes/addobits/{year}/{m}/{apiKey}";

                    Console.WriteLine($"{year} {m}: processing request. Please wait...\r\n");
                    HttpResponseMessage response = await client.GetAsync(uri);

                    string message = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                        Console.WriteLine(ConsoleColor.Green, message);
                    else
                        throw new ArgumentException(message);
                }
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

        private void TestConfigSetting()
        {
            System.Console.WriteLine(configuration.GetValue<string>("NYTimes Archive API key"));
        }

        private void TestGetDeceasedPerDay()
        {
            //TODO: per month
            DateTime deathDate = new DateTime(2005, 5, 12);

            var entries = wikipediaService.GetDeceased(deathDate);

            Console.WriteLine($"Count: {entries.Count()}");
        }
    }
}
