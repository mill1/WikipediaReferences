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
