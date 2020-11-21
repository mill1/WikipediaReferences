using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
