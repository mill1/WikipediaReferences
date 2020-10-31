using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace WikipediaReferences.Models
{
    public class Article
    {
        //private Article CreateArticle(int monthId, int year, Doc obituaryDoc, string articleTitle)
        //{
        //    return new Article()
        //    {
        //        articleTitle = articleTitle,
        //        type = "Obituary",
        //        lastname = GetLastName(articleTitle),
        //        reference = CreateReference(obituaryDoc),
        //        deathdate = GetDateOfDeath(obituaryDoc, monthId, year)
        //    };
        //}
        //public override string ToString()
        //{
        //    return $"[[{articleTitle}]]{reference}";
        //}

        // TODO name style Camel case
        public int Id { get; set; }
        public string type { get; set; }
        public string SourceCode { get; set; }
        public string articleTitle { get; set; } // = wiki LINKED name
        public string lastname { get; set; }        
        public string author1 { get; set; }
        public string authorlink1 { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public string urlaccess { get; set; }
        public string work { get; set; }
        public DateTime accessdate { get; set; }
        public DateTime date { get; set; }
        public string page { get; set; }
        public DateTime deathdate { get; set; }

        public string GetNewsReference()
        {
            var ci = new CultureInfo("en-US");

            return "<ref>{{cite news" +
                    $" |author1={author1}" +
                    $" |authorlink1={authorlink1}" +
                    $" |title={title}" +
                    $" |url={url.Replace(@"\/", "/")}" + // unescape / (although never escaped)
                    $" |url-access={urlaccess}" +
                    $" |access-date={accessdate.ToString("d MMMM yyyy", ci)}" +
                    $" |work={work}" +
                    $" |date={date.ToString("d MMMM yyyy", ci)}" +
                    $" |page={page}" +
                   "}}</ref>";
        }
    }
}
