using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using WikipediaReferences.Dtos;

namespace WikipediaReferences.Console.Services
{
    public class NytReferencesEditor
    {
        private readonly IConfiguration configuration;
        private readonly Util util;

        public NytReferencesEditor(IConfiguration configuration, Util util)
        {
            this.configuration = configuration;
            this.util = util;
        }

        public void ShowNYTimesUrlOfArticle()
        {
            try
            {
                UI.Console.WriteLine("Article title:");
                string articleTitle = UI.Console.ReadLine();

                IEnumerable<Reference> references = GetReferencesByArticleTitle(articleTitle);

                references.ToList().ForEach(r =>
                    {
                        var reference = MapDtoToModel(r);

                        UI.Console.WriteLine(ConsoleColor.Green, reference.GetNewsReference());
                    });


            }
            catch (WikipediaReferencesException e)
            {
                UI.Console.WriteLine(ConsoleColor.Magenta, e.Message);
            }
            catch (Exception e)
            {
                UI.Console.WriteLine(ConsoleColor.Red, e);
            }
        }

        private IEnumerable<Reference> GetReferencesByArticleTitle(string articleTitle)
        {
            string uri = $"nytimes/referencebyarticletitle/{articleTitle.Replace(" ", "_")}";
            HttpResponseMessage response = util.SendGetRequest(uri);

            string result = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
                return JsonConvert.DeserializeObject<IEnumerable<Reference>>(result);
            else
                throw new WikipediaReferencesException(result);
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
                    throw new WikipediaReferencesException(result);
            }
            catch (WikipediaReferencesException e)
            {
                UI.Console.WriteLine(ConsoleColor.Magenta, e.Message);
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

            UI.Console.WriteLine("New date of death: (yyyy-m-d)");
            updateDeathDate.DeathDate = DateTime.Parse(UI.Console.ReadLine());

            UI.Console.WriteLine("Article title:");
            updateDeathDate.ArticleTitle = UI.Console.ReadLine();

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
                    throw new WikipediaReferencesException(result);
            }
            catch (WikipediaReferencesException e)
            {
                UI.Console.WriteLine(ConsoleColor.Magenta, e.Message);
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
                UI.Console.WriteLine(ApiKey + ":");
                apiKey = UI.Console.ReadLine();
            }

            return $"nytimes/addobits/{year}/{monthId}/{apiKey}";
        }

        private WikipediaReferences.Models.Reference MapDtoToModel(Reference referenceDto)
        {
            return new WikipediaReferences.Models.Reference
            {
                Id = referenceDto.Id,
                Type = referenceDto.Type,
                SourceCode = referenceDto.SourceCode,
                ArticleTitle = referenceDto.ArticleTitle,
                LastNameSubject = referenceDto.LastNameSubject,
                Author1 = referenceDto.Author1,
                Authorlink1 = referenceDto.Authorlink1,
                Title = referenceDto.Title,
                Url = referenceDto.Url,
                UrlAccess = referenceDto.UrlAccess,
                Quote = referenceDto.Quote,
                Work = referenceDto.Work,
                Agency = referenceDto.Agency,
                Publisher = referenceDto.Publisher,
                Language = referenceDto.Language,
                Location = referenceDto.Location,
                AccessDate = referenceDto.AccessDate,
                Date = referenceDto.Date,
                Page = referenceDto.Page,
                DeathDate = referenceDto.DeathDate,
                ArchiveDate = referenceDto.ArchiveDate,
            };
        }
    }
}
