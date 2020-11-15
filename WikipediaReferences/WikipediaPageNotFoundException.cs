using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WikipediaReferences
{
    public class WikipediaPageNotFoundException : Exception
    {
        public WikipediaPageNotFoundException()
        {
        }

        public WikipediaPageNotFoundException(string message)
            : base(message)
        {
        }

        public WikipediaPageNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
