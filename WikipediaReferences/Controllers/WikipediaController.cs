﻿using System;
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
    public class WikipediaController : ControllerBase
    {

        private readonly IWikipediaService wikipediaService;
        private readonly ILogger<WikipediaController> logger;

        public WikipediaController(IWikipediaService wikipediaService, ILogger<WikipediaController> logger)
        {
            this.wikipediaService = wikipediaService;
            this.logger = logger;
        }

        [HttpGet("deceased/{date}")]
        public IActionResult GetDeceasedByDate(DateTime date)
        {
            try
            {
                return Ok(wikipediaService.GetDeceased(date));
            }
            catch (Exception e)
            {
                string message = $"Getting the deceased by date failed. Requested date of death: {date.ToShortDateString()}.\r\n" +
                                 $"Exception:\r\n{e}";
                logger.LogError($"{message}", e);
                return BadRequest(message);
            }
        }
    }
}
