using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using WikipediaReferences.Dtos;

namespace WikipediaConsole
{
    public class ReferencesEditor
    {
        private readonly IConfiguration configuration;
        private readonly Util util;        

        public ReferencesEditor(IConfiguration configuration, Util util)
        {
            this.configuration = configuration;
            this.util = util;            
        }

        public void ShowNYTimesUrlOfArticle()
        {
            try
            {
                Console.WriteLine("Article title:");
                string articleTitle = Console.ReadLine();

                IEnumerable<Reference> references = GetReferencesByArticleTitle(articleTitle);

                references.ToList().ForEach(r => UI.Console.WriteLine(ConsoleColor.Green, r.Url));                
            }
            catch (ArgumentException e)
            {
                UI.Console.WriteLine(ConsoleColor.Magenta, e);
            }
            catch (Exception e)
            {
                UI.Console.WriteLine(ConsoleColor.Red, e);
            }
        }

        private IEnumerable<Reference> GetReferencesByArticleTitle(string articleTitle)
        {
            string uri = $"nytimes/referencebyarticletitle/{ articleTitle.Replace(" ", "_") }";
            HttpResponseMessage response = util.SendGetRequest(uri);
            string result = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
                return JsonConvert.DeserializeObject<IEnumerable<Reference>>(result);
            else
                throw new ArgumentException(result);
        }

        public void UpdateNYTDeathDateOfReference()
        {
            try
            {
                UpdateDeathDate updateDeathDate = GetUpdateDeathDateDto();
                HttpResponseMessage response = util.SendPutRequest("nytimes/updatedeathdate", updateDeathDate);

                string result = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                    ShowUpdatedDeathDate(result);
                else
                    throw new Exception(result);
            }
            catch (Exception e)
            {
                UI.Console.WriteLine(ConsoleColor.Red, e);
            }
        }

        private void ShowUpdatedDeathDate(string result)
        {
            var updateDeathDate = JsonConvert.DeserializeObject<UpdateDeathDate>(result);

            UI.Console.WriteLine(ConsoleColor.Green, $"Updated death date: {updateDeathDate.DeathDate.ToShortDateString()}");
            UI.Console.WriteLine(ConsoleColor.Green, $"Article subject: {updateDeathDate.ArticleTitle} (1st id: {updateDeathDate.Id})");
        }

        private UpdateDeathDate GetUpdateDeathDateDto()
        {
            UpdateDeathDate updateDeathDate = new UpdateDeathDate() { SourceCode = "NYT" };

            Console.WriteLine("New date of death: (yyyy-m-d)");
            updateDeathDate.DeathDate = DateTime.Parse(Console.ReadLine());

            Console.WriteLine("Article title:");
            updateDeathDate.ArticleTitle = Console.ReadLine();

            return updateDeathDate;
        }

        public void AddNYTimesObituaryReferences()
        {
            try
            {
                string uri = GetAddObitsApiUri();
                HttpResponseMessage response = util.SendGetRequest(uri);
                string result = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                    UI.Console.WriteLine(ConsoleColor.Green, result);
                else
                    throw new ArgumentException(result);

            }
            catch (ArgumentException e)
            {
                UI.Console.WriteLine(ConsoleColor.Magenta, e);
            }
            catch (Exception e)
            {
                UI.Console.WriteLine(ConsoleColor.Red, e);
            }
        }

        private string GetAddObitsApiUri()
        {
            const string ApiKey = "NYTimes Archive API key";

            int year, monthId;
            util.GetDeathMontArgs(out year, out monthId);

            string apiKey = configuration.GetValue<string>(ApiKey);

            if (apiKey == null || apiKey == "TOSET")
            {
                Console.WriteLine(ApiKey + ":");
                apiKey = Console.ReadLine();
            }

            return $"nytimes/addobits/{year}/{monthId}/{apiKey}";
        }
    }
}
