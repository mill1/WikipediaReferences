using System;

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
