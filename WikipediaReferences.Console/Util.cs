using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Wikimedia.Utilities.Exceptions;
using WikipediaReferences.Dtos;

namespace WikipediaReferences.Console
{
    public class Util
    {
        private readonly HttpClient client;

        public Util(IConfiguration configuration, HttpClient client)
        {
            this.client = client;
            // TODO: uitzoeken waarom configuration.GetValue niet meer werkt.
            var uri = configuration.GetValue<string>("WRWebApi:SchemeAndHost");
            uri = uri ?? "https://localhost:44385";
            this.client.BaseAddress = new Uri(uri);
            this.client.DefaultRequestHeaders.Accept.Clear();
            this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public string GetRawArticleText(string articleTitle, bool netto)
        {
            // https://en.wikipedia.org/wiki/Help!:_A_Day_in_the_Life
            articleTitle = articleTitle.Replace("/", "%2F");
            articleTitle = articleTitle.Replace(":", "%3A");

            string uri = $"wikipedia/rawarticle/{articleTitle}/netto/{netto}";
            HttpResponseMessage response = SendGetRequest(uri);

            return HandleResponse(response, articleTitle);
        }

        public string HandleResponse(HttpResponseMessage response, string articleTitle)
        {
            string result = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
                return result;
            else
            {
                if (result.Contains(typeof(WikipediaPageNotFoundException).Name))
                    throw new WikipediaReferencesException($"\r\nArticle '{articleTitle}' does not exist (anymore) on Wikipedia.");
                else
                    throw new HttpRequestException($"\r\nArticle: {articleTitle} result: '{result}'");
            }
        }

        public void GetDeathMontArgs(out int year, out int monthId)
        {
            UI.Console.WriteLine("Death year:");
            year = int.Parse(UI.Console.ReadLine());
            UI.Console.WriteLine("Death month id:");
            monthId = int.Parse(UI.Console.ReadLine());
        }

        public HttpResponseMessage SendGetRequest(string uri)
        {
            return client.GetAsync(uri).Result;
        }

        public HttpResponseMessage SendPutRequest(string uri, UpdateDeathDate updateDeathDate)
        {
            return client.PutAsJsonAsync(uri, updateDeathDate).Result;
        }
    }
}
