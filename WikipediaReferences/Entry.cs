using System;
using System.Collections.Generic;
using System.Linq;

namespace WikipediaReferences
{
    public class Entry
    {
        public string Name { get; set; }        
        public string LinkedName { get; set; }
        public string Information { get; set; }
        public string Reference { get; set; }
        public DateTime DeathDate { get; set; }

        public override string ToString()
        {
            string name = (LinkedName == Name) ? Name : $"{LinkedName}|{Name}";
            return $"*[[{name}]], {Information}{Reference}";
        }
    }
}
