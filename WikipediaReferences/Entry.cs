using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            string linkedName = (Name == LinkedName) ? "," : LinkedName + ", ";
            string reference = (Reference == null) ? String.Empty : "<ref>...";

            return $"{DeathDate.ToShortDateString()}: {Name}{linkedName} {Information} {reference}";
        }
    }
}
