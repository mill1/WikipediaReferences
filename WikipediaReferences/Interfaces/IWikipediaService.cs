using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WikipediaReferences.Models;

namespace WikipediaReferences.Interfaces
{
    public interface IWikipediaService
    {
        public IEnumerable<Entry> GetEntries(DateTime date);
    }
}
