using System;
using System.Globalization;

namespace WikipediaReferences.Models
{
    public class Reference
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string SourceCode { get; set; }
        public string ArticleTitle { get; set; }      // = Wiki LINKED name
        public string LastNameSubject { get; set; }   // Subject = ArticleTitle = Bio   
        public string Author1 { get; set; }
        public string Authorlink1 { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string UrlAccess { get; set; }
        public string Quote { get; set; }
        public string Work { get; set; }
        public string Agency { get; set; }
        public string Publisher { get; set; }
        public string Language { get; set; }
        public string Location { get; set; }
        public DateTime AccessDate { get; set; }
        public DateTime Date { get; set; }
        public string Page { get; set; }
        public DateTime DeathDate { get; set; }
        public DateTime ArchiveDate { get; set; }

        public string GetNewsReference()
        {
            var ci = new CultureInfo("en-US");

            return "<ref>{{cite news" +
                    $" |author1={Author1}" +
                    (string.IsNullOrEmpty(Authorlink1) ? "" : $" |author-link1={Authorlink1}") +
                    $" |title={Title}" +
                    $" |url={Url.Replace(@"\/", "/")}" + // unescape / (although never escaped)
                    $" |url-access={UrlAccess}" +
                    $" |access-date={AccessDate.ToString("d MMMM yyyy", ci)}" +
                    $" |work=[[{Work}]]" +
                    $" |date={Date.ToString("d MMMM yyyy", ci)}" +
                    $" |page={Page}" +
                   //$" |language={Language}" +
                   //$" |quote={Quote}" +
                   "}}</ref>";
        }
    }
}
