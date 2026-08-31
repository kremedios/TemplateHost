using System.Text.Json;
using System.Text.RegularExpressions;

namespace Host.Services.Logging
{
    public class BotDetector
    {
        private readonly List<Regex> _botPatterns;

        public BotDetector(string jsonFilePath)
        {
            var json = File.ReadAllText(jsonFilePath);
            var entries = JsonSerializer.Deserialize<List<CrawlerEntry>>(json);

            _botPatterns = entries
                .Select(e => new Regex(e.Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled))
                .ToList();
        }

        public bool IsKnownBot(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return true; // no UA at all is suspicious — treat as bot

            return _botPatterns.Any(p => p.IsMatch(userAgent));
        }

        private class CrawlerEntry
        {
            public string Pattern { get; set; } = string.Empty;
            public string? Url { get; set; }
            public List<string>? Instances { get; set; }
        }
    }
}