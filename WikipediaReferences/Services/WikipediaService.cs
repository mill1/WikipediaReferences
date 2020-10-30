using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WikipediaReferences.Interfaces;
using WikipediaReferences.Models;

namespace WikipediaReferences.Services
{
    public class WikipediaService : IWikipediaService
    {
        private const string UrlWikipediaRawBase = "https://en.wikipedia.org/w/index.php?action=raw&title="; // title=Deaths_in_May_2005
        private const string EntryDelimiter = "*[[";

        public IEnumerable<Entry> GetEntries(DateTime deathDate)
        {
            string text;
            string month = deathDate.ToString("MMMM", new CultureInfo("en-US"));

            using (WebClient client = new WebClient())
                text = client.DownloadString(UrlWikipediaRawBase + $"Deaths_in_{month}_{deathDate.Year}");

            if (text.Contains("* [[")) // We only want '*[[' (without the space); edit article in that case
                throw new Exception("Invalid markup style found: * [[");

            text = TrimWikiText(text, month, deathDate.Year);
            text = GetDaySection(text, deathDate.Day, false);

            IEnumerable<string> rawEntries = GetRawEntries(text);
            IEnumerable<Entry> entries = rawEntries.Select(e => ParseEntry(e, deathDate));

            return entries;
        }

        private Entry ParseEntry(string rawEntry, DateTime deathDate)
        {
            return new Entry
            {
                LinkedName = GetNameFromRawEntry(rawEntry, true),
                Name = GetNameFromRawEntry(rawEntry, false),
                Information = GetInformationFromRawEntry(rawEntry),
                Reference = GetReferenceFromRawEntry(rawEntry),
                DeathDate = deathDate                
            };
        }

        private string GetInformationFromRawEntry(string rawEntry)
        {
            string info = rawEntry.Substring(rawEntry.IndexOf("]]") + "]]".Length);

            // Loose the first comma
            info = info.Substring(1).Trim();

            int posRef = info.IndexOf("<ref>");

            if (posRef < 0)
                return info;
            else
                return info.Substring(0, posRef);
        }

        private string GetReferenceFromRawEntry(string rawEntry)
        {
            int pos = rawEntry.IndexOf("<ref>");

            if (pos < 0)
                return null;
            else
                return rawEntry.Substring(pos);
        }

        private string GetNameFromRawEntry (string rawEntry, bool linkedName)
        {
            string namePart = rawEntry.Substring("[[".Length, rawEntry.IndexOf("]]") - "]]".Length);
            int pos = namePart.IndexOf('|');

            if (pos < 0)
                return namePart;
            else
            {
                if (linkedName)
                    return namePart.Substring(0, pos);
                else
                    return namePart.Substring(pos + "|".Length);
            }
        }

        private IEnumerable<string> GetRawEntries(string daySection)
        {
            string[] array = daySection.Split(EntryDelimiter);

            IEnumerable<string> rawEntries = array.Select( e => "[[" +  e);

            return rawEntries.Skip(1);
        }

        private string GetDaySection(string wikiText, int day, bool trimHeader)
        {
            string daySection = wikiText;
            int pos;

            //Trim left
            pos = Math.Max(daySection.IndexOf($"==={day}==="), daySection.IndexOf($"=== {day} ==="));
            daySection = daySection.Substring(pos);

            if (trimHeader)
                daySection = daySection.Substring(daySection.IndexOf(EntryDelimiter));

            // Trim right
            pos = Math.Max(daySection.IndexOf($"==={day + 1}==="), daySection.IndexOf($"=== {day + 1} ==="));

            if (pos < 0) // we reached the end
                return daySection;
            else
                return daySection.Substring(0, pos);
        }

        private string TrimWikiText(string wikiText, string month, int year)
        {
            string trimmedText = wikiText;
            int pos;

            //Trim left
            pos = Math.Max(trimmedText.IndexOf($"=={month} {year}=="), trimmedText.IndexOf($"== {month} {year} =="));
            trimmedText = trimmedText.Substring(pos);

            // Trim right
            pos = Math.Max(trimmedText.IndexOf("==References=="), trimmedText.IndexOf("== References =="));
            trimmedText = trimmedText.Substring(0, pos);

            // Loose '\n'
            trimmedText = trimmedText.Replace("\n", "");

            return trimmedText;
        }
    }
}
