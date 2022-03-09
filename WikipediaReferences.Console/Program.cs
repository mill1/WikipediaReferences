using Microsoft.Extensions.DependencyInjection;
using System;
using WikipediaReferences.Console.UI;

namespace WikipediaReferences.Console
{
    public class Program
    {
        // STEP 1: Parse wikipedia list article as json: done
        // STEP 2: Create a database with obituary reference info on persons that have a wiki: done
        // STEP 3: Per day of a specific month:
        // Per reference find the matching entry in the day subsection of the wiki list (should be found; db only contains refs that have a wiki bio)
        // If found check if a reference already exists for the entry
        // No reference:  add it to the entry
        // Optional: if existing reference replace it (if it is from the NYT)
        // If no matching entry was found: investigate; check the entire month. ALL references should be accounted for.
        // STEP 4: Generate the new version of the list article including the added/replaced references

        public static void Main()
        {
            IServiceCollection services = new ServiceCollection();
            new Startup().ConfigureServices(services);
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            var runner = serviceProvider.GetService<Runner>();
            runner.Run();
        }
    }
}
