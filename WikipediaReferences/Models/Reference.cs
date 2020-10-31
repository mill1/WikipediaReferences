using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace WikipediaReferences.Models
{
    public class Reference
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string SourceCode { get; set; }
        public string ArticleTitle { get; set; } // = wiki LINKED name
        public string Lastname { get; set; }        
        public string Author1 { get; set; }
        public string Authorlink1 { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Urlaccess { get; set; }
        public string Work { get; set; }
        public DateTime Accessdate { get; set; }
        public DateTime Date { get; set; }
        public string Page { get; set; }
        public DateTime Deathdate { get; set; }

        public string GetNewsReference()
        {
            var ci = new CultureInfo("en-US");

            return "<ref>{{cite news" +
                    $" |author1={Author1}" +
                    $" |authorlink1={Authorlink1}" +
                    $" |title={Title}" +
                    $" |url={Url.Replace(@"\/", "/")}" + // unescape / (although never escaped)
                    $" |url-access={Urlaccess}" +
                    $" |access-date={Accessdate.ToString("d MMMM yyyy", ci)}" +
                    $" |work={Work}" +
                    $" |date={Date.ToString("d MMMM yyyy", ci)}" +
                    $" |page={Page}" +
                   "}}</ref>";
        }
    }
}
