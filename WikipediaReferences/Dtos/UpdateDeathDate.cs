using System;

namespace WikipediaReferences.Dtos
{
    public class UpdateDeathDate
    {
        public int Id { get; set; }
        public string SourceCode { get; set; }
        public string ArticleTitle { get; set; }      // = Wiki LINKED name
        public DateTime DeathDate { get; set; }
    }
}
