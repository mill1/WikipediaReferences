using System;
using WikipediaReferences.Interfaces;
using WikipediaReferences.Services;
using WikipediaReferences.Sources;
using Xunit;

namespace WikipediaReferences.Tests
{
    public class NYTimesServiceShould
    {

        [Theory(DisplayName = "get the death date from the document")]
        [InlineData("2006-1-31", "Nam June Paik..., died Sunday at his winter home in Miami Beach.", "2006-1-29")]
        [InlineData("1900-1-9", "John Doe died Jan. 2", "1900-1-2")]
        [InlineData("1901-1-1", "John Doe died Dec. 26", "1900-12-26")]
        [InlineData("1900-1-1", "John Doe died today", "1900-1-1")]
        [InlineData("1900-1-1", "John Doe was killed by X today", "1900-1-1")]
        [InlineData("1900-1-1", "John Doe found dead by X today", "1900-1-1")]
        [InlineData("1900-1-1", "John Doe died this morning ", "1900-1-1")]
        [InlineData("1900-1-1", "John Doe died this afternoon", "1900-1-1")]
        [InlineData("1900-1-1", "John Doe died this evening", "1900-1-1")]
        [InlineData("1900-1-2", "John Doe died early yesterday", "1900-1-1")]
        [InlineData("1900-6-30", "John Doe died on Jan. 1", "1900-1-1")]
        [InlineData("1901-3-31", "John Doe died on Oct. 1", "1900-10-1")]
        public void ResolveDateOfDeathFromExcerpt(string publicationDateAsString, string leadParagraph, string expected)
        {
            DateTime publicationDate = DateTime.Parse(publicationDateAsString);

            INYTimesService service = new NYTimesService(null, null);
            Doc doc = CreateDoc(publicationDate, leadParagraph);

            var deathDate = service.ResolveDateOfDeath(doc, publicationDate.Month, publicationDate.Year);

            Assert.Equal(DateTime.Parse(expected), deathDate);
        }

        [Fact(DisplayName = "resolve DoD Alexandra Gardiner Creel")]
        // issue: https://www.nytimes.com/1990/12/18/obituaries/a-gardiner-creel-80-island-s-co-owner-dies.html : yesterday AND 'died in July' (the husband)
        public void ResolveDateOfDeathFromExcerptX()
        {
            INYTimesService service = new NYTimesService(null, null);
            DateTime publicationDate = DateTime.Parse("1990-12-18T05:00:00+0000");

            var doc = new Doc
            {
                pub_date = publicationDate,
                @abstract = "  Alexandra Gardiner Creel, the co-owner of Gardiners Island, believed to be the largest privately owned island in America, died yesterday at North Shore University Hospital in Glen Cove, L.I. She was 80 years old and lived in Mill Creek, L.I.   She died of lung disease, said her brother, Robert David Lion Gardiner of Palm Beach, Fla.    Mrs. Creel and her brother inherited the 3,300-acre island on the eastern end of Long Island, which was bestowed on her family by King Charles I in 1639. She was involved in many social and charitable organizations, including the Colonial Dames of America.    Her death may send Gardiners Island back into the courts. Mr. Gardiner, the island's other owner, and Mrs. Creel's daughter, Alexandra Gardiner Creel Goelet of Manhattan, have been arguing for the past decade over zoning and other environmental issues on the island.   In addition to her brother and her daughter, Mrs. Creel is survived by four grandchildren. Her husband, James Randall Creel, a retired judge of the New York City Criminal Court, died in July.",
                lead_paragraph = "Alexandra Gardiner Creel, the co-owner of Gardiners Island, believed to be the largest privately owned island in America, died yesterday at North Shore University Hospital in Glen Cove, L.I. She was 80 years old and lived in Mill Creek, L.I."
            };

            var deathDate = service.ResolveDateOfDeath(doc, publicationDate.Month, publicationDate.Year);
            var expected = publicationDate.AddDays(-1).Date;  // 'yesterday'

            Assert.Equal(expected, deathDate);
        }

        private Doc CreateDoc(DateTime publicationDate, string excerpt)
        {
            return new Doc()
            {
                pub_date = publicationDate.Date,
                @abstract = null,
                lead_paragraph = excerpt
            };
        }
    }
}
