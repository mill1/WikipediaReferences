using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using WikipediaReferences.Dtos;
using WikipediaReferences;

namespace WikipediaConsole
{
    public class Util
    {
        private readonly IConfiguration configuration;
        private readonly HttpClient client;

        public string HandleResponse(HttpResponseMessage response, string articleTitle)
        {
            string result = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
                return result;
            else
            {
                if (result.Contains(typeof(ReferencesNotFoundException).Name))
                    throw new ReferencesNotFoundException($"Article '{articleTitle}' does not exist (anymore) on Wikipedia.");
                else
                    throw new Exception(result);
            }
        }

        public Util(IConfiguration configuration, HttpClient client)
        {
            this.configuration = configuration;

            this.client = client;
            var uri = configuration.GetValue<string>("WRWebApi:SchemeAndHost");
            this.client.BaseAddress = new Uri(uri);
            this.client.DefaultRequestHeaders.Accept.Clear();
            this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void GetDeathMontArgs(out int year, out int monthId)
        {
            Console.WriteLine("Death year:");
            year = int.Parse(Console.ReadLine());
            Console.WriteLine("Death month id:");
            monthId = int.Parse(Console.ReadLine());
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
