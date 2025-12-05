using System;

namespace WikipediaReferences
{
    public class EntryDetails
    {
        // writer.Write($"Year;MonthId;Day;Name;Age;Nationality;Occupation;CoD;InformationPart\r\n");
        public DateTime DeathDate { get; set; }
        public string Name { get; set; }
        public string Age { get; set; }
        public string Nationality { get; set; }
        public string Occupation { get; set; }
        public string CauseOfDeath { get; set; }
        public string InformationPart { get; set; }

        public override string ToString()
        {
            return $"{DeathDate.Year};{DeathDate.Month};{DeathDate.Day};{Name};{Age};{Nationality};{Occupation};{CauseOfDeath};{InformationPart}";
        }
    }
}
