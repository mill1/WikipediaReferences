using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace WikipediaConsole.Services
{
    public class ArticleAnalyzer
    {
        private readonly Util util;
        public ArticleAnalyzer(Util util)
        {
            this.util = util;
        }

        public bool ArticleContainsSublist(string articleTitle)
        {
            string uri = $"wikipedia/rawarticle/{articleTitle}/netto/true";
            HttpResponseMessage response = util.SendGetRequest(uri);

            string rawArticleText = util.HandleResponse(response, articleTitle);

            return rawArticleText.Contains("**[[");
        }

        public void ShowRawArticleText(bool netto)
        {
            try
            {
                UI.Console.WriteLine("Article title:");
                string articleTitle = Console.ReadLine();

                string uri = $"wikipedia/rawarticle/{articleTitle}/netto/{netto}";
                HttpResponseMessage response = util.SendGetRequest(uri);

                string rawArticleText = util.HandleResponse(response, articleTitle);

                UI.Console.WriteLine(ConsoleColor.Green, rawArticleText);
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
    }
}
