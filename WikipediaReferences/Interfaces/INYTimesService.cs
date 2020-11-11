using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WikipediaReferences.Models;

namespace WikipediaReferences.Interfaces
{
    public interface INYTimesService
    {
        public IEnumerable<Reference> GetReferencesPerDeathDate(DateTime deathDate);
        public IEnumerable<Reference> GetReferencesPerMonthOfDeath(int year, int monthId);
        public string AddObituaryReferences(int year, int month, string apiKey);
    }
}
