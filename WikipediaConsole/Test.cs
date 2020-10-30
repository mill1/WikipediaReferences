using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WikipediaReferences.Interfaces;
using WikipediaReferences.Services;

namespace WikipediaConsole
{
    class Test
    {
        public void Run()
        {
            TestWikipediaService();
        }

        private void TestWikipediaService()
        {
            IWikipediaService wikipediaService = new WikipediaService();

            DateTime deathDate = new DateTime(2005, 5, 12);

            var entries = wikipediaService.GetEntries(deathDate);

            Console.WriteLine($"Count: {entries.Count()}");
        }

        private void TestWeatherForecastService()
        {
            IWeatherForecastService weatherForecast = new WeatherForecastService();

            var list = weatherForecast.GetWeatherForecasts();

            Console.WriteLine($"Count: {list.Count()}");
        }
    }
}
