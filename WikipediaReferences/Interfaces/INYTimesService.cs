using System;
using System.Collections.Generic;
using WikipediaReferences.Models;
using WikipediaReferences.Sources;

namespace WikipediaReferences.Interfaces
{
    public interface INYTimesService
    {
        public IEnumerable<Reference> GetReferencesPerDeathDate(DateTime deathDate);
        public IEnumerable<Reference> GetReferencesPerMonthOfDeath(int year, int monthId);
        public string AddObituaryReferences(int year, int month, string apiKey);
        public IEnumerable<Reference> GetReferencesByArticleTitle(string articleTitle);
        public Reference UpdateDeathDate(IEnumerable<Reference> references, Dtos.UpdateDeathDate updateDeathDateDto);
        public DateTime ResolveDateOfDeath(Doc obituaryDoc, int monthId, int year);
    }
}
