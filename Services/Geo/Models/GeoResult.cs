namespace Host.Services.Geo.Models
{
    public class GeoResult
    {
        public string IpAddress { get; set; } = "";
        public string Country { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public bool IsFake { get; set; }
    }
}
