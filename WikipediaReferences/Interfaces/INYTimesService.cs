﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WikipediaReferences.Interfaces
{
    public interface INYTimesService
    {
        public void AddNYTimesObituaryReferences(int year, int month, string apiKey);
    }
}