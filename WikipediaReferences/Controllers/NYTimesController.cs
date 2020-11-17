using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WikipediaReferences.Interfaces;
//using WikipediaReferences.Models;

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

        [HttpGet("reference/{deathDate}")]
        public IActionResult GetReferencesPerDeathDate(DateTime deathDate)
        {
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

        [HttpGet("reference/{year}/{monthId}")]
        public IActionResult GetReferencesPerMonthOfDeath(int year, int monthId)
        {
            try
            {
                IEnumerable<Models.Reference> references = nyTimesService.GetReferencesPerMonthOfDeath(year, monthId);

                if (references.Count() == 0)
                    return NotFound($"No references were found. Requested month: {year} {monthId}");

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
                string message = nyTimesService.AddObituaryReferences(year, monthId, apikey);
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

            if (references.Count() == 0)
                return NotFound($"Reference(s) was not found. Requested article title = {updateDeathDateDto.ArticleTitle}.");
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

        private Models.Reference MapDtoToModel(Dtos.Reference referenceDto, Models.Reference reference)
        {
            reference.Id = referenceDto.Id;
            reference.Type = referenceDto.Type;
            reference.SourceCode = referenceDto.SourceCode;
            reference.ArticleTitle = referenceDto.ArticleTitle;
            reference.LastNameSubject = referenceDto.LastNameSubject;
            reference.Author1 = referenceDto.Author1;
            reference.Authorlink1 = referenceDto.Authorlink1;
            reference.Title = referenceDto.Title;
            reference.Url = referenceDto.Url;
            reference.UrlAccess = referenceDto.UrlAccess;
            reference.Quote = referenceDto.Quote;
            reference.Work = referenceDto.Work;
            reference.Agency = referenceDto.Agency;
            reference.Publisher = referenceDto.Publisher;
            reference.Language = referenceDto.Language;
            reference.Location = referenceDto.Location;
            reference.AccessDate = referenceDto.AccessDate;
            reference.Date = referenceDto.Date;
            reference.Page = referenceDto.Page;
            reference.DeathDate = referenceDto.DeathDate;
            reference.ArchiveDate = referenceDto.ArchiveDate;

            return reference;
        }
    }
}
