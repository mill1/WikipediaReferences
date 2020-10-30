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
    public class WeatherForecastController : ControllerBase
    {

        private readonly IWeatherForecastService weatherForecastService;
        private readonly ILogger<WeatherForecastController> logger;

        public WeatherForecastController(IWeatherForecastService weatherForecastService, ILogger<WeatherForecastController> logger)
        {
            this.weatherForecastService = weatherForecastService;
            this.logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            return weatherForecastService.GetWeatherForecasts();
        }
    }
}
