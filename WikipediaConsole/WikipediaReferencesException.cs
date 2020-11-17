using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
