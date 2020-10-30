using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WikipediaReferences.Models;

namespace WikipediaReferences.Data
{
    public class SeedData
    {
        public IEnumerable<Source> Sources { get; set; }
    }
}
