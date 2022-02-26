using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
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

        [HttpGet("rawarticle/{articleTitle}/netto/{nettoContent}")]
        public IActionResult GetArticle(string articleTitle, bool nettoContent)
        {
            try
            {
                return Ok(wikipediaService.GetRawArticleText(articleTitle, nettoContent));
            }
            catch (Exception e)
            {
                string message = $"Getting the raw article text failed. Requested article: {articleTitle}.\r\n" +
                                 $"Exception:\r\n{e}";
                logger.LogError($"{message}", e);
                return BadRequest(message);
            }
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

        [HttpGet("deceased/{year}/{monthId}")]
        public IActionResult GetDeceasedByMonth(int year, int monthId)
        {
            try
            {
                return Ok(wikipediaService.GetDeceased(year, monthId));
            }
            catch (Exception e)
            {
                string message = $"Getting the deceased by month failed. Requested month of death: {monthId} {year}.\r\n" +
                                 $"Exception:\r\n{e}";
                logger.LogError($"{message}", e);
                return BadRequest(message);
            }
        }
    }
}
