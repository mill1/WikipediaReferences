using System;

namespace WikipediaReferences
{
    public class InvalidWikipediaPageException : Exception
    {
        public InvalidWikipediaPageException()
        {
        }

        public InvalidWikipediaPageException(string message)
            : base(message)
        {
        }

        public InvalidWikipediaPageException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
