﻿using System;
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
        public string ArticleTitle { get; set; }      // = Wiki LINKED name
        public string LastNameSubject { get; set; }   // Subject = ArticleTitle = Bio   
        public string Author1 { get; set; }
        public string Authorlink1 { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string UrlAccess { get; set; }
        public string Work { get; set; }
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
                    $" |authorlink1={Authorlink1}" +
                    $" |title={Title}" +
                    $" |url={Url.Replace(@"\/", "/")}" + // unescape / (although never escaped)
                    $" |url-access={UrlAccess}" +
                    $" |access-date={AccessDate.ToString("d MMMM yyyy", ci)}" +
                    $" |work={Work}" +
                    $" |date={Date.ToString("d MMMM yyyy", ci)}" +
                    $" |page={Page}" +
                   "}}</ref>";
        }
    }
}
