﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WikipediaReferences.Models
{
    public class Reference
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SourceCode { get; set; }
        public int SomeInteger { get; set; }
    }
}
