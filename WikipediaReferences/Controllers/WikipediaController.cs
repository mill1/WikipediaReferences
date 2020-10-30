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
    public class WikipediaController : ControllerBase
    {

        private readonly IWikipediaService wikipediaService;
        private readonly ILogger<WikipediaController> logger;

        public WikipediaController(IWikipediaService wikipediaService, ILogger<WikipediaController> logger)
        {
            this.wikipediaService = wikipediaService;
            this.logger = logger;
        }

        [HttpGet("{date}")]
        public IActionResult GetEntriesByDate(DateTime date)
        {
            try
            {
                return Ok(wikipediaService.GetEntries(date));
            }
            catch (Exception e)
            {
                string message = $"Getting the entries by date failed. Requested date of death: {date.ToShortDateString()}.";
                logger.LogError($"{message} Exception:\r\n{e}", e);
                return BadRequest(message);
            }
        }
    }
}
