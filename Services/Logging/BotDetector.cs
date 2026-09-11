using System.Text.Json;
using System.Text.RegularExpressions;

namespace Host.Services.Logging
{
    public class BotDetector
    {
        private readonly List<(Regex Pattern, string Name)> _botEntries;

        // Curated list of pattern names considered genuinely harmful
        // (attack tools, vulnerability/exploit scanners).
        private static readonly HashSet<string> _harmfulPatternNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Nikto",
            "sqlmap",
            "ZmEu",
            "masscan",
            "WPScan",
            "[aA]cunetix",
            "Nessus",
            "[dD]ir[Bb]uster",
            "zgrab",
            "l9scan",
            "l9explore",
            "CensysInspect\\/",
            "Nmap Scripting Engine",
            "ec2linkfinder",
            "ip-web-crawler\\.com",
            "SBL-BOT",
            "binlar",
            "K7MLWCBot",
            "UT-Dorkbot",
            "filterdb\\.iss\\.net\\/crawler",
            "WanscannerBot"
        };

        public BotDetector(string jsonFilePath)
        {
            var json = File.ReadAllText(jsonFilePath);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var entries = JsonSerializer.Deserialize<List<CrawlerEntry>>(json, options);

            _botEntries = new List<(Regex, string)>();
            foreach (var e in entries)
            {
                try
                {
                    var regex = new Regex(e.Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    _botEntries.Add((regex, e.Pattern));
                }
                catch (ArgumentException)
                {
                    // skip malformed pattern, optionally log
                }
            }
        }

        public bool IsKnownBot(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return true; // no UA at all is suspicious — treat as bot

            return _botEntries.Any(entry => entry.Pattern.IsMatch(userAgent));
        }

        public bool IsHarmfulBot(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return false; // no UA is suspicious, but not confirmed "harmful" via pattern match

            return _botEntries.Any(entry =>
                _harmfulPatternNames.Contains(entry.Name) &&
                entry.Pattern.IsMatch(userAgent));
        }

        private class CrawlerEntry
        {
            public string Pattern { get; set; } = string.Empty;
            public string? Url { get; set; }
            public List<string>? Instances { get; set; }
        }
    }
}