using System;
using System.Net.Http;

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
            string rawArticleText = GetRawArticleText(articleTitle, true);

            return rawArticleText.Contains("**[[");
        }

        public void ShowRawArticleText(bool netto)
        {
            try
            {
                UI.Console.WriteLine("Article title:");
                string articleTitle = Console.ReadLine();
                
                string rawArticleText = GetRawArticleText(articleTitle, netto);

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

        private string GetRawArticleText(string articleTitle, bool netto)
        {
            // https://en.wikipedia.org/wiki/Help!:_A_Day_in_the_Life
            articleTitle = articleTitle.Replace("/", "%2F");
            articleTitle = articleTitle.Replace(":", "%3A");

            string uri = $"wikipedia/rawarticle/{articleTitle}/netto/{netto}";
            HttpResponseMessage response = util.SendGetRequest(uri);

            return util.HandleResponse(response, articleTitle);
        }
    }
}
