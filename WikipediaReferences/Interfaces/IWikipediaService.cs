using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WikipediaReferences.Interfaces
{
    public interface IWikipediaService
    {
        public IEnumerable<Entry> GetDeceased(DateTime date);
        public IEnumerable<Entry> GetDeceased(int year, int month);
        public string GetArticleTitle(string nameVersion, int year, int monthId);
        public string GetAuthorsArticle(string author, string source);
        public string GetRawArticleText(ref string article, bool nettoContent, bool printNotFound);
    }
}
