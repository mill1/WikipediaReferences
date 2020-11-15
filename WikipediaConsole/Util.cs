using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace WikipediaConsole
{
    public class Util
    {
        private readonly IConfiguration configuration;
        private readonly HttpClient client;

        public Util(IConfiguration configuration, HttpClient client)
        {
            this.configuration = configuration;

            this.client = client;
            var uri = configuration.GetValue<string>("WRWebApi:SchemeAndHost");
            this.client.BaseAddress = new Uri(uri);
            this.client.DefaultRequestHeaders.Accept.Clear();
            this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public string SendGetRequest(string uri, out HttpResponseMessage response)
        {
            //Console.WriteLine("Processing request. Please wait...");
            response = client.GetAsync(uri).Result;

            return response.Content.ReadAsStringAsync().Result;
        }
    }
}
