﻿using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Wikimedia.Utilities.Exceptions;
using WikipediaReferences.Dtos;

namespace WikipediaConsole
{
    public class Util
    {
        private readonly HttpClient client;

        public Util(IConfiguration configuration, HttpClient client)
        {
            this.client = client;
            var uri = configuration.GetValue<string>("WRWebApi:SchemeAndHost");
            this.client.BaseAddress = new Uri(uri);
            this.client.DefaultRequestHeaders.Accept.Clear();
            this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public string HandleResponse(HttpResponseMessage response, string articleTitle)
        {
            string result = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
                return result;
            else
            {
                if (result.Contains(typeof(WikipediaPageNotFoundException).Name))
                    throw new WikipediaReferencesException($"Article '{articleTitle}' does not exist (anymore) on Wikipedia.");
                else
                    throw new Exception($"Article: {articleTitle} result: '{result}'");
            }
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
