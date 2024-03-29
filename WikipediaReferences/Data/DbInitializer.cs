﻿using Newtonsoft.Json;
using System.IO;
using System.Linq;

namespace WikipediaReferences.Data
{
    public static class DbInitializer
    {
        public static void Initialize(WRContext context)
        {
            SeedData seedData;

            context.Database.EnsureCreated();

            if (context.Sources.Any())
                return;   // DB has been seeded

            string path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "SeedData.json");

            using (StreamReader r = new StreamReader(path))
            {
                string json = r.ReadToEnd();
                seedData = JsonConvert.DeserializeObject<SeedData>(json);
            }

            foreach (var source in seedData.Sources)
                context.Sources.Add(source);

            context.SaveChanges();
        }
    }
}
