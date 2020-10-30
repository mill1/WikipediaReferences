using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WikipediaReferences.Models
{
    public class DataType
    {
        public DataType()
        {
            Attributes = new List<Attribute>();
        }

        public string Code { get; set; }
        public string Name { get; set; }

        public virtual ICollection<Attribute> Attributes { get; set; }
    }
}
