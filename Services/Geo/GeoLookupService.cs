using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Responses;

namespace Host.Services.Geo
{
    public class GeoLookupService
    {
        private readonly string _dbPath;
        private readonly DatabaseReader _reader;

        public GeoLookupService(string dbPath)
        {
            _reader = new DatabaseReader(dbPath);
        }

        public GeoLocation Lookup(string ip)
        {
            try
            {
                CityResponse response = _reader.City(ip);

                var city = response.City?.Name ?? "-";
                var state = response.MostSpecificSubdivision?.Name ?? "-";
                var country = response.Country?.Name ?? "-";

                return new GeoLocation
                {
                    City = city,
                    State = state,
                    Country = country
                };
            }
            catch
            {
                // Return default values if lookup fails
                return new GeoLocation();
            }
        }
    }
}
