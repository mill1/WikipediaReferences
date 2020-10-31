using Microsoft.Extensions.DependencyInjection;
using WikipediaConsole.UI;
using System;

namespace WikipediaConsole
{
    public class Program
    {
        // STEP 1: parse wikipedia list article as json: done
        // STEP 2: Create a database with obituary reference info on persons that have a wiki
        // STEP 3: Per day of a specific month per entry check for references in the database
        // Process per entry of a day sub section:
        // If found Check if reference already exists for an entry add it to the entry
        // Optional: if existing reference replace it (if it is from the NYT)
        // If not found check for db reference on another day of the month (/ year?). If so investigate mismatch dates
        // STEP 4: Generate the new version of the list article including the added/replaced references

        public static async System.Threading.Tasks.Task Main(string[] args)
        {
            IServiceCollection services = new ServiceCollection();
            new Startup().ConfigureServices(services);
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            var runner = serviceProvider.GetService<Runner>();
            runner.Run();
        }  
    }
}
