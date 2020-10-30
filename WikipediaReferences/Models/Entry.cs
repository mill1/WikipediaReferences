using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WikipediaReferences.Models
{
    public class Entry
    {
        public string Name { get; set; }        
        public string LinkedName { get; set; }
        public string Information { get; set; }
        public string Reference { get; set; }
        public DateTime DeathDate { get; set; }
    }
}
