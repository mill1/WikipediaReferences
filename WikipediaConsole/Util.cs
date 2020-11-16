using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using WikipediaReferences.Dtos;

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
