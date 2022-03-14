using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using WikipediaReferences.Interfaces;

namespace WikipediaReferences.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NYTimesController : ControllerBase
    {

        private readonly INYTimesService nyTimesService;
        private readonly ILogger<NYTimesController> logger;

        public NYTimesController(INYTimesService nyTimesService, ILogger<NYTimesController> logger)
        {
            this.nyTimesService = nyTimesService;
            this.logger = logger;
        }

        [HttpGet("referencebyarticletitle/{articleTitle}")]
        public IActionResult GetReferencesByArticleTitle(string articleTitle)
        {
            if (string.IsNullOrEmpty(articleTitle))
                return BadRequest("articleTitle cannot be null or empty.");

            articleTitle = articleTitle.Replace("_", " ");

            IEnumerable<Models.Reference> references = nyTimesService.GetReferencesByArticleTitle(articleTitle);

            if (!references.Any())
                return NotFound($"Reference(s) not found. Requested article title = {articleTitle}.");
            try
            {
                return Ok(MapModelToDto(references));
            }
            catch (Exception e)
            {
                string message = $"Getting the reference(s) failed. Article title = {articleTitle}.";
                logger.LogError($"{message} Exception:\r\n{e}", e);
                return BadRequest(message);
            }
        }

        [HttpGet("references/{deathDate}")]
        public IActionResult GetReferencesPerDeathDate(DateTime deathDate)
        {
            // Not being caled yet from the client.
            try
            {
                IEnumerable<Models.Reference> references = nyTimesService.GetReferencesPerDeathDate(deathDate);

                return Ok(references);
            }
            catch (Exception e)
            {
                string message = $"Getting the references failed. Requested death date: {deathDate.ToShortDateString()}.\r\n" +
                                 $"Exception:\r\n{e}";
                logger.LogError($"{message} Exception:\r\n{e}", e);
                return BadRequest(message);
            }
        }

        [HttpGet("references/{year}/{monthId}")]
        public IActionResult GetReferencesPerMonthOfDeath(int year, int monthId)
        {
            try
            {
                IEnumerable<Models.Reference> references = nyTimesService.GetReferencesPerMonthOfDeath(year, monthId);

                if (!references.Any())
                    return NotFound($"References not found. Requested month: {year} {monthId}");

                return Ok(references);
            }
            catch (Exception e)
            {
                string message = $"Getting the references failed. Requested month: {year} {monthId}.\r\n" +
                                 $"Exception:\r\n{e}";
                logger.LogError($"{message} Exception:\r\n{e}", e);
                return BadRequest(message);
            }
        }

        [HttpGet("addobits/{year}/{monthId}/{apikey}")]
        public IActionResult AddObituaryReferences(int year, int monthId, string apikey)
        {
            try
            {
                string message = "";
                //for (year = 1991; year > 1988; year--)
                //{
                //    for (monthId = 1; monthId <= 12; monthId++)
                //    {
                        message = nyTimesService.AddObituaryReferences(year, monthId, apikey);
                //        Console.WriteLine($"############ month {monthId}: {message}");
                //    }
                //}
                
                return Ok(message);
            }
            catch (Exception e)
            {
                string message = $"Adding the NYTimes references failed. Requested month: {year} {monthId}.\r\n" +
                                 $"Exception:\r\n{e}";

                logger.LogError($"{message}", e);
                return BadRequest(message);
            }
        }

        [HttpPut("updatedeathdate")]
        public IActionResult UpdateDeathDate(Dtos.UpdateDeathDate updateDeathDateDto)
        {
            if (updateDeathDateDto == null)
                return BadRequest("Dto object cannot be null.");

            IEnumerable<Models.Reference> references = nyTimesService.GetReferencesByArticleTitle(updateDeathDateDto.ArticleTitle);

            if (!references.Any())
                return NotFound($"Reference(s) not found. Requested article title = {updateDeathDateDto.ArticleTitle}.");
            try
            {
                var reference = nyTimesService.UpdateDeathDate(references, updateDeathDateDto);

                return Ok(MapModelToDto(reference, reference.DeathDate));
            }
            catch (Exception e)
            {
                string message = $"Updating the reference(s) failed. Article title = {updateDeathDateDto.ArticleTitle}.";
                logger.LogError($"{message} Exception:\r\n{e}", e);
                return BadRequest(message);
            }
        }

        private Dtos.UpdateDeathDate MapModelToDto(Models.Reference reference, DateTime deathDate)
        {
            return new Dtos.UpdateDeathDate
            {
                Id = reference.Id,
                SourceCode = reference.SourceCode,
                ArticleTitle = reference.ArticleTitle,
                DeathDate = deathDate
            };
        }

        private IEnumerable<Dtos.Reference> MapModelToDto(IEnumerable<Models.Reference> references)
        {
            List<Dtos.Reference> referencesDto = new List<Dtos.Reference>();

            foreach (var reference in references)
                referencesDto.Add(MapModelToDto(reference));

            return referencesDto;
        }

        private Dtos.Reference MapModelToDto(Models.Reference reference)
        {
            return new Dtos.Reference
            {
                Id = reference.Id,
                Type = reference.Type,
                SourceCode = reference.SourceCode,
                ArticleTitle = reference.ArticleTitle,
                LastNameSubject = reference.LastNameSubject,
                Author1 = reference.Author1,
                Authorlink1 = reference.Authorlink1,
                Title = reference.Title,
                Url = reference.Url,
                UrlAccess = reference.UrlAccess,
                Quote = reference.Quote,
                Work = reference.Work,
                Agency = reference.Agency,
                Publisher = reference.Publisher,
                Language = reference.Language,
                Location = reference.Location,
                AccessDate = reference.AccessDate,
                Date = reference.Date,
                Page = reference.Page,
                DeathDate = reference.DeathDate,
                ArchiveDate = reference.ArchiveDate,
            };
        }
    }
}
