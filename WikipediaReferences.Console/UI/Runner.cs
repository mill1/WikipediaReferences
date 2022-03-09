using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using WikipediaReferences.Console.Services;
using WikipediaReferences;

namespace WikipediaReferences.Console.UI
{
    public class Runner
    {
        private const string PrintDeathMonth = "p";
        private const string GenerateRef = "r";
        private const string GenerateRefNYT = "s";
        private const string GenerateRefOlympedia = "o";
        private const string UpdateNYTDeathDate = "u";
        private const string DayCheck = "d";
        private const string AddNYTObitRefs = "a";
        private const string NumberOfNettoChars = "n";
        private const string Test = "t";
        private const string Quit = "q";
        private bool quit;

        private readonly Util util;
        private readonly ListArticleGenerator listArticleGenerator;
        private readonly ReferencesEditor referencesEditor;

        public Runner(ListArticleGenerator listArticleGenerator, ReferencesEditor referencesEditor,
                      Util util, AssemblyInfo assemblyInfo)
        {
            this.util = util;
            this.listArticleGenerator = listArticleGenerator;
            this.referencesEditor = referencesEditor;
            quit = false;

            var assemblyName = assemblyInfo.GetAssemblyName();
            string appVersion = assemblyInfo.GetAssemblyValue("Version", assemblyName);

            Console.DisplayAssemblyInfo(assemblyName.Name, appVersion);
        }

        public void Run()
        {
            while (!quit)
            {
                Console.DisplayMenu(ConsoleColor.Yellow, GetMenuItems());

                string answer = Console.ReadLine();

                try
                {
                    ProcessAnswer(answer);
                }
                catch (Exception e)
                {
                    Console.WriteLine(ConsoleColor.Red, e);
                }
            }
        }

        private List<string> GetMenuItems()
        {
            return new List<string>
            {
                $"{PrintDeathMonth}:\tPrint month of death",
                $"{GenerateRef}:\tGenerate reference",
                $"{UpdateNYTDeathDate}:\tUpdate date of death",
                $"{DayCheck}:\tDay name of date",
                $"{AddNYTObitRefs}:\tAdd NYT obituaries to db",
                $"{NumberOfNettoChars}:\tNumber of netto chars of article",
                $"{Test}:\tTest stuff",
                $"{Quit}:\tQuit"
            };
        }

        private void GenerateReference()
        {
            string answer = "";
            do
            {
                Console.DisplayMenu(ConsoleColor.Yellow, GetMenuItemsGenRefs());
                answer = Console.ReadLine();
                ProcessAnswerGenRef(answer);

            }while(answer != Quit);
        }

        private List<string> GetMenuItemsGenRefs()
        {
            return new List<string>
            {
                $"{GenerateRefNYT}:\tShow NYT Url of article",
                $"{GenerateRefOlympedia}:\tGenerate Olympedia ref",
                $"{Quit}:\tExit"
            };
        }

        private void ProcessAnswerGenRef(string answer)
        {
            switch (answer)
            {
                case GenerateRefNYT:
                    referencesEditor.GenerateReferenceNYT();
                    break;
                case GenerateRefOlympedia:
                    referencesEditor.GenerateOlympediaReference();
                    break;
                case Quit:
                    System.Console.WriteLine();
                    break;
                default:
                    Console.WriteLine($"Invalid choice: {answer}");
                    break;
            }
        }


        private void ProcessAnswer(string answer)
        {
            int year, monthId;

            switch (answer)
            {
                case PrintDeathMonth:
                    util.GetDeathMontArgs(out year, out monthId);
                    listArticleGenerator.PrintDeathsPerMonthArticle(year, monthId);
                    break;
                case UpdateNYTDeathDate:
                    referencesEditor.UpdateNYTDeathDateOfReference();
                    break;
                case GenerateRef:
                    GenerateReference();
                    break;
                case DayCheck:
                    GetDayNameFromDate();
                    break;
                case GenerateRefOlympedia:
                    referencesEditor.GenerateOlympediaReference();
                    break;
                case Test:
                    TestGetDeceasedFromWikipedia();
                    break;
                case AddNYTObitRefs:
                    referencesEditor.AddNYTimesObituaryReferences();
                    break;
                case NumberOfNettoChars:
                    listArticleGenerator.DetermineNumberOfCharactersBiography();
                    break;
                case Quit:
                    quit = true;
                    break;
                default:
                    Console.WriteLine($"Invalid choice: {answer}");
                    break;
            }
        }

        private void GetDayNameFromDate()
        {
            Console.WriteLine("Date: (yyyy-M-d)");
            DateTime date = DateTime.Parse(Console.ReadLine());

            Console.WriteLine(ConsoleColor.Blue, date.DayOfWeek.ToString());
        }

        private void TestGetDeceasedFromWikipedia()
        {
            string uri = $"wikipedia/deceased/1999-5-8/Deaths_in_May_1999";
            HttpResponseMessage response = util.SendGetRequest(uri);
            string result = response.Content.ReadAsStringAsync().Result;

            IEnumerable<Entry> entries = JsonConvert.DeserializeObject<IEnumerable<Entry>>(result);
            var refs = entries.Where(e => e.Reference != null);

            int maxLength = entries.Max(e => e.Information.Length);
            Entry entry = entries.First(e => e.Information.Length == maxLength);

            Console.WriteLine($"Nr of entries: {entries.Count()}");
            Console.WriteLine($"Nr of entries with references: {refs.Count()}");
            Console.WriteLine($"Longest entry (excl. ref): {entry.Name}");
            Console.WriteLine($"Longest entry value:\r\n{entry}");
        }
    }
}
