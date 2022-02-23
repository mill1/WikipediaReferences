using System;

namespace WikipediaReferences.Sources
{
    // https://developer.nytimes.com/docs/archive-product/1/overview

    public class NYTimesArchive
    {
        public string copyright { get; set; }
        public Response response { get; set; }
    }

    public class Response
    {
        public Meta meta { get; set; }
        public Doc[] docs { get; set; }
    }

    public class Meta
    {
        public int hits { get; set; }
    }

    public class Doc
    {
        public string @abstract { get; set; }
        public string web_url { get; set; }
        public string snippet { get; set; }
        public string lead_paragraph { get; set; }
        public string print_section { get; set; }
        public string print_page { get; set; }
        public string source { get; set; }
        public object[] multimedia { get; set; }
        public Headline headline { get; set; }
        public Keyword[] keywords { get; set; }
        public DateTime pub_date { get; set; }
        public string document_type { get; set; }
        public string news_desk { get; set; }
        public string section_name { get; set; }
        public Byline byline { get; set; }
        public string type_of_material { get; set; }
        public string _id { get; set; }
        public int word_count { get; set; }
        public string uri { get; set; }
        public string subsection_name { get; set; }
    }

    public class Headline
    {
        public string main { get; set; }
        public string kicker { get; set; }
        public object content_kicker { get; set; }
        public string print_headline { get; set; }
        public object name { get; set; }
        public object seo { get; set; }
        public object sub { get; set; }
    }

    public class Byline
    {
        public string original { get; set; }
        public Person[] person { get; set; }
        public object organization { get; set; }
    }

    public class Person
    {
        public string firstname { get; set; }
        public string middlename { get; set; }
        public string lastname { get; set; }
        public string qualifier { get; set; }
        public object title { get; set; }
        public string role { get; set; }
        public string organization { get; set; }
        public int rank { get; set; }
    }

    public class Keyword
    {
        public string name { get; set; }
        public string value { get; set; }
        public int rank { get; set; }
        public string major { get; set; }
    }
}
