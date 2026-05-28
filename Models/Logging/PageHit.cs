namespace Host.Models.Logging
{
    public class PageHit
    {
        public string PageName { get; set; } = string.Empty;
        public DateTime HitTimeCentral { get; set; }

        public string IpAddress { get; set; } = string.Empty;
        public string Device { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
    }
}

