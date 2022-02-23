using System;

namespace WikipediaConsole
{
    public class WikipediaReferencesException : Exception
    {
        public WikipediaReferencesException()
        {
        }

        public WikipediaReferencesException(string message)
            : base(message)
        {
        }

        public WikipediaReferencesException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
