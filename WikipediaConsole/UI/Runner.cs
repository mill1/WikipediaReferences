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
        private const string RawArticletext = "r";
        private const string Fix1995Phase1 = "1";
        private const string Fix1995Phase2 = "2";
        private const string Test = "t";
        private const string AddNYTObitRefs = "a";
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
                $"{RawArticletext}:\tDisplay raw text of article",
                $"{Test}:\tTest stuff",
                $"{Fix1995Phase1}:\tTemp: Fix 1995 Phase 1",
                $"{Fix1995Phase2}:\tTemp: Fix 1995 Phase 2",
                $"{AddNYTObitRefs}:\tAdd NYT obituaries to db",
                $"{Quit}:\tQuit application"
            };
        }

        private void ProcessAnswer(string answer)
        {
            int year, monthId;

            switch (answer)
            {
                case Test:
                    string s = ((char)150).ToString();
                    System.Console.WriteLine(s);
                    break;
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
                case RawArticletext:
                    articleAnalyzer.ShowRawArticleText(false);
                    break;
                case Fix1995Phase1:
                    Console.WriteLine("Death month id:");
                    listArticleGenerator.Fix1995Phase1(int.Parse(Console.ReadLine()));
                    break;
                case Fix1995Phase2:
                    Console.WriteLine("Death month id:");
                    listArticleGenerator.Fix1995Phase2(int.Parse(Console.ReadLine()));
                    break;               
                case AddNYTObitRefs:
                    referencesEditor.AddNYTimesObituaryReferences();
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
             
    }
}
