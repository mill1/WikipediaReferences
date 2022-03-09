using System;
using System.Net.Http;

namespace WikipediaReferences.Console.Services
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
            string rawArticleText = util.GetRawArticleText(articleTitle, true);

            return rawArticleText.Contains("**[[");
        }
    }
}
