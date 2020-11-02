using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
