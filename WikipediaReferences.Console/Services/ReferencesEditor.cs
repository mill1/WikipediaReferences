﻿using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using WikipediaReferences.Dtos;

namespace WikipediaReferences.Console.Services
{
    public class ReferencesEditor
    {
        private readonly IConfiguration configuration;
        private readonly WebClient webClient;
        private readonly Util util;

        public ReferencesEditor(IConfiguration configuration, WebClient webClient, Util util)
        {
            this.configuration = configuration;
            this.webClient = webClient;
            this.webClient.Headers.Clear();
            this.webClient.Headers.Add("User-Agent", "C# application");
            this.util = util;
        }

        public void GenerateOlympediaReference()
        {
            string url = GetReferenceUrl("http://www.olympedia.org/athletes/", "Olympedia Id: (f.i.: 73711)");
            var rootNode = GetHtmlDocRootNode(url);

            var table = rootNode.Descendants(0).First(n => n.HasClass("biodata"))
                .Descendants("tr")
                .Select(tr =>
                {
                    var key = tr.Elements("th").Select(td => td.InnerText).First();
                    var value = tr.Elements("td").Select(td => td.InnerText).First();
                    return new KeyValuePair<string, string>(key, value);
                }
                ).ToList();

            string usedName = table.First(kvp => kvp.Key == "Used name").Value.Replace("•", " ");
            var reference = GenerateWebReference($"Olympedia – {usedName}", url, "olympedia.org", DateTime.Today, DateTime.MinValue, publisher: "[[OlyMADMen]]");

            UI.Console.WriteLine(ConsoleColor.Green, reference);
        }

        private string GetReferenceUrl(string urlBase, string message)
        {
            UI.Console.WriteLine(message);
            string id = UI.Console.ReadLine();

            return $"{urlBase}{id}";
        }

        private HtmlNode GetHtmlDocRootNode(string url)
        {
            var response = webClient.DownloadString(url);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(response);
            return doc.DocumentNode;
        }
        public void GenerateLoCReference()
        {            
            // https://id.loc.gov/authorities/names/no98081061.html
            var url = GetReferenceUrl("https://id.loc.gov/authorities/names/", $"LoC id: (f.i.: 'no98081061' )") + ".html";
            var rootNode = GetHtmlDocRootNode(url);
            // <h1><span property="madsrdf:authoritativeLabel skos:prefLabel">Root Boy Slim</span></h1>
            var title = rootNode.SelectSingleNode("//h1").ChildNodes.First().InnerText;
            title += " - Library of Congress";

            var reference = GenerateWebReference(title, url, "id.loc.gov", DateTime.Today, DateTime.MinValue);

            UI.Console.WriteLine(ConsoleColor.Green, reference);
        }

        public void GenerateBaseballReference()
        {
            GenerateSportsReference("baseball-reference.com", "b/bellbu01", ".shtml");
        }

        public void GenerateBasketballReference()
        {
            GenerateSportsReference("basketball-reference.com", "b/bellra01", ".html");
        }

        public void GenerateFootballReference()
        {
            GenerateSportsReference("pro-football-reference.com", "B/BellBi21", ".htm");
        }

        public void GenerateHockeyReference()
        {
            GenerateSportsReference("hockey-reference.com", "b/bellbr01", ".html");
        }

        public void GenerateCricketReference()
        {
            string urlBase = "https://www.espncricinfo.com/cricketers/";
            string playerIdExample = "pochiah-krishnamurthy-30135";
            string url = GetReferenceUrl(urlBase, $"Player id: (f.i.: '{playerIdExample}' )");
            string playerId = url.Replace(urlBase, string.Empty);
            string title = GenerateTitle(playerId);

            var reference = GenerateWebReference(title, url, "[[ESPNcricinfo]]", DateTime.Today, DateTime.MinValue);

            UI.Console.WriteLine(ConsoleColor.Green, reference);
        }

        private string GenerateTitle(string playerIdExample)
        {
            string title = string.Empty;
            string[] nameParts = playerIdExample.Split('-');

            if (nameParts.Length == 1)
                throw new ArgumentException("id should contain at least one '-' chartacter");

            for (int i = 0; i < nameParts.Length-1; i++)
                title += FirstLetterToUpper(nameParts[i]) + " ";

            title += "profile and biography, stats, records, averages, photos and videos";

            return title;
        }

        private string FirstLetterToUpper(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);

            return str.ToUpper();
        }

        public void GenerateSportsReference(string domain, string playerIdExample, string urlSuffix)
        {
            string urlBase = "https://www." + domain + "/players/";
            string url = GetReferenceUrl(urlBase, $"Player id: (f.i.: '{playerIdExample}' )") + urlSuffix;
            var rootNode = GetHtmlDocRootNode(url);

            var title = rootNode.SelectSingleNode("//head/title").InnerText;
            title = title.Replace("|", "&ndash;");

            var reference = GenerateWebReference(title, url, domain, DateTime.Today, DateTime.MinValue);

            UI.Console.WriteLine(ConsoleColor.Green, reference);
        }

        private string GenerateWebReference(string title, string url, string website, DateTime accessDate, DateTime date,
                                       string last1 = "", string first1 = "", string publisher = "", string language = "")
        {
            var cultureInfo = new CultureInfo("en-US");

            return "<ref>{{cite web" +
                    (last1 == "" ? "" : $" |last1={last1}") +
                    (first1 == "" ? "" : $" |first1={first1}") +
                    $" |title={title}" +
                    $" |url={url.Replace(@"\/", "/")}" + // unescape / (although never escaped)
                    $" |website={website}" +
                    (publisher == "" ? "" : $" |publisher={publisher}") +
                    (language == "" ? "" : $" |language={language}") +
                    (date == DateTime.MinValue ? "" : $" |date={date.ToString("d MMMM yyyy", cultureInfo)}") +
                    $" |access-date={accessDate.ToString("d MMMM yyyy", cultureInfo)}" +
                   "}}</ref>";
        }

        public void GenerateReferenceNYT()
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

            UI.Console.WriteLine("New date of death: (dd-MM-yyyy)");
            
            updateDeathDate.DeathDate = DateTime.ParseExact(UI.Console.ReadLine(), "dd-MM-yyyy", CultureInfo.InvariantCulture);

            UI.Console.WriteLine("Article title:");
            updateDeathDate.ArticleTitle = UI.Console.ReadLine();

            return updateDeathDate;
        }

        public void AddNYTimesObituaryReferences()
        {
            try
            {
                int year = 0, monthId = 0;

                UI.Console.WriteLine("Process entire year? (y/n)");

                bool processEntireYear = UI.Console.ReadLine() == "y";

                string apiKey = GetNYTimesApiKey();

                if (processEntireYear)
                {
                    year = util.GetDeathYearArg();

                    for (monthId = 1; monthId <= 12; monthId++)
                    {
                        DisplayStatus(monthId);
                        AddNYTimesObituaryReferencesOfMonth(year, monthId, apiKey);
                    }
                }
                else
                {                    
                    util.GetDeathYearMonthArgs(out year, out monthId);
                    DisplayStatus(monthId);
                    AddNYTimesObituaryReferencesOfMonth(year, monthId, apiKey);
                }
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

        private static void DisplayStatus(int monthId)
        {
            UI.Console.WriteLine(ConsoleColor.Green, $"Processing month {monthId}...");
        }

        private void AddNYTimesObituaryReferencesOfMonth(int year, int monthId, string apiKey)
        {
            string uri = GetAddObitsApiUri(year, monthId, apiKey);
            HttpResponseMessage response = util.SendGetRequest(uri);
            string result = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
                UI.Console.WriteLine(ConsoleColor.Green, result);
            else
                throw new WikipediaReferencesException(result);
        }

        private string GetAddObitsApiUri(int year, int monthId, string apiKey)
        {            
            return $"nytimes/addobits/{year}/{monthId}/{apiKey}";
        }

        private string GetNYTimesApiKey()
        {
            const string ApiKey = "NYTimes Archive API key";

            string apiKey = configuration.GetValue<string>(ApiKey);

            if (apiKey == null || apiKey == "TOSET")
            {
                UI.Console.WriteLine(ApiKey + ":");
                apiKey = UI.Console.ReadLine();
            }

            return apiKey;
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
