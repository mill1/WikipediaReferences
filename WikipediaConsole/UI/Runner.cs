
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IWikipediaService wikipediaService;
        private readonly INYTimesService nyTimesService;

        public Runner(IConfiguration configuration, IWikipediaService wikipediaService, INYTimesService nyTimesService, AssemblyInfo assemblyInfo)
        {
            this.configuration = configuration;
            this.wikipediaService = wikipediaService;
            this.nyTimesService = nyTimesService;

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

        private void AddNYTimesObituaryReferences()
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
                    Console.WriteLine(ApiKey+":");
                    apiKey = Console.ReadLine();
                }

                nyTimesService.AddNYTimesObituaryReferences(year, monthId, apiKey);
            }
            catch (Exception e)
            {
                Console.WriteLine(ConsoleColor.Red, e);
            }
        }

        private void TestConfigSetting()
        {
            System.Console.WriteLine(configuration.GetValue<string>("NYTime Archive API key"));
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
