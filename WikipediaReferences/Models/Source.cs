﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WikipediaReferences.Models
{
    public class Source
    {
        public Source()
        {
            References = new List<Reference>();
        }

        public string Code { get; set; }
        public string Name { get; set; }

        public virtual ICollection<Reference> References { get; set; }
    }
}
