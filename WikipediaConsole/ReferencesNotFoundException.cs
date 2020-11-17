using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WikipediaConsole
{
    public class ReferencesNotFoundException : Exception
    {
        public ReferencesNotFoundException()
        {
        }

        public ReferencesNotFoundException(string message)
            : base(message)
        {
        }

        public ReferencesNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
