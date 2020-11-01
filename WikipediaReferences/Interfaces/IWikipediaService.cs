using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WikipediaReferences.Interfaces
{
    public interface IWikipediaService
    {
        public IEnumerable<Entry> GetDeceased(DateTime date);
        public string GetArticleTitle(string nameVersion, int year, int monthId);
        public string GetAuthorsArticle(string author, string source);
    }
}
