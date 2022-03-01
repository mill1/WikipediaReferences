using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using WikipediaConsole.Services;
using WikipediaReferences;

namespace WikipediaConsole.UI
{
    public class Runner
    {
        private const string PrintDeathMonth = "p";
        private const string UpdateNYTDeathDate = "u";
        private const string ShowNYTUrl = "s";
        private const string DayCheck = "d";
        private const string Test = "t";
        private const string AddNYTObitRefs = "a";
        private const string NumberOfNettoChars = "n";
        private const string Quit = "q";
        private bool quit;

        private readonly Util util;
        private readonly ListArticleGenerator listArticleGenerator;
        private readonly ReferencesEditor referencesEditor;
        private readonly ArticleAnalyzer articleAnalyzer;

        public Runner(ListArticleGenerator listArticleGenerator, ReferencesEditor referencesEditor, ArticleAnalyzer articleAnalyzer,
                      Util util, AssemblyInfo assemblyInfo)
        {
            this.util = util;
            this.listArticleGenerator = listArticleGenerator;
            this.referencesEditor = referencesEditor;
            this.articleAnalyzer = articleAnalyzer;

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
                $"{UpdateNYTDeathDate}:\tUpdate date of death",
                $"{ShowNYTUrl}:\tShow NYT Url of article",
                $"{DayCheck}:\tDay name of date",
                $"{Test}:\tTest stuff",
                $"{AddNYTObitRefs}:\tAdd NYT obituaries to db",
                $"{NumberOfNettoChars}:\tNumber of netto chars of article",
                $"{Quit}:\tQuit application"
            };
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
                case ShowNYTUrl:
                    referencesEditor.ShowNYTimesUrlOfArticle();
                    break;
                case DayCheck:
                    GetDaynameFromDate();
                    break;
                case Test:
                    articleAnalyzer.ShowRawArticleText(false);
                    //TestGetDeceasedFromWikipedia();
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

        private void GetDaynameFromDate()
        {
            Console.WriteLine("Date: (yyyy-M-d)");
            DateTime date = DateTime.Parse(Console.ReadLine());

            Console.WriteLine(ConsoleColor.Blue, date.DayOfWeek.ToString());
        }

        private void TestGetDeceasedFromWikipedia()
        {
            string uri = $"wikipedia/deceased/1999-5-8";
            HttpResponseMessage response = util.SendGetRequest(uri);
            string result = response.Content.ReadAsStringAsync().Result;

            IEnumerable<Entry> entries = JsonConvert.DeserializeObject<IEnumerable<Entry>>(result);
            var refs = entries.Where(e => e.Reference != null);

            int maxLength = entries.Max(e => e.Information.Length);
            Entry entry = entries.Where(e => e.Information.Length == maxLength).First();

            Console.WriteLine($"Nr of entries: {entries.Count()}");
            Console.WriteLine($"Nr of entries with references: {refs.Count()}");
            Console.WriteLine($"Longest entry (excl. ref): {entry.Name}");
            Console.WriteLine($"Longest entry value:\r\n{entry}");
        }
    }
}
