using System;
using System.Collections.Generic;

namespace WikipediaReferences.Interfaces
{
    public interface IWikipediaService
    {
        public IEnumerable<Entry> GetDeceased(DateTime deathDate);
        public IEnumerable<Entry> GetDeceased(int year, int monthId);
        public string GetArticleTitle(string nameVersion, int year, int monthId);
        public string GetAuthorsArticle(string author, string source);
        public string GetRawArticleText(ref string article, bool nettoContent);
    }
}
