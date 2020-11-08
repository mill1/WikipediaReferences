using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WikipediaReferences.Interfaces;
using WikipediaReferences.Models;

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
        public IActionResult GetReferencePerDeathDate(DateTime deathDate)
        {
            try
            {
                IEnumerable<Reference> references = nyTimesService.GetReferencesPerDeathDate(deathDate);

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
        public IActionResult GetReferencePerArchiveMonth(int year, int monthId)
        {
            try
            {
                IEnumerable<Reference> references = nyTimesService.GetReferencesPerArchiveMonth(year, monthId);

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
                string message = $"Adding the NYTimes obituary references failed. Requested month: {year} {monthId}.\r\n" +
                                 $"Exception:\r\n{e}";
                logger.LogError($"{message}", e);
                return BadRequest(message);
            }
        }
    }
}
