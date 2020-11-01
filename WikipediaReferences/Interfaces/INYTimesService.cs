using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WikipediaReferences.Interfaces
{
    public interface INYTimesService
    {
        public string AddObituaryReferences(int year, int month, string apiKey);
    }
}
