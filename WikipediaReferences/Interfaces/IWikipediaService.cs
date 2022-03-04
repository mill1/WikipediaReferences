using System;
using System.Collections.Generic;

namespace WikipediaReferences.Interfaces
{
    public interface IWikipediaService
    {
        public IEnumerable<Entry> GetDeceased(DateTime deathDate, string articleTitle = null);
        public IEnumerable<Entry> GetDeceased(int year, int monthId, string articleTitle = null);
        public string GetArticleTitle(string nameVersion, int year, int monthId);
        public string GetAuthorsArticle(string author, string source);
        public string GetRawArticleText(string article, bool nettoContent);
    }
}
