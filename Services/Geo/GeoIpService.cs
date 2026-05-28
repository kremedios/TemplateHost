using System.Net;
using Host.Services.Geo.Models;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Responses;

namespace Host.Services.Geo
{
    public class GeoIpService
    {
        private readonly DatabaseReader _reader;

        public GeoIpService(string databasePath)
        {
            _reader = new DatabaseReader(databasePath);
        }

        public CityResponse GetCity(string ipAddress)
        {
            return _reader.City(ipAddress);
        }

        public GeoResult Lookup(string ipAddress)
        {
            // Handle localhost / private IPs with fake data
            if (IsPrivateIp(ipAddress))
            {
                return new GeoResult
                {
                    IpAddress = ipAddress,
                    Country = "United States",
                    State = "Localhost state",   // fake state for private IPs
                    City = "Localhost city",
                    IsFake = true
                };
            }

            try
            {
                var city = _reader.City(ipAddress);

                return new GeoResult
                {
                    IpAddress = ipAddress,
                    Country = city.Country?.Name ?? "",
                    State = city.MostSpecificSubdivision?.Name ?? "",   // <-- populate state
                    City = city.City?.Name ?? "",
                    IsFake = false
                };
            }
            catch
            {
                // Fallback safety net
                return new GeoResult
                {
                    IpAddress = ipAddress,
                    Country = "Unknown",
                    State = "Unknown",
                    City = "Unknown",
                    IsFake = true
                };
            }
        }



        private bool IsPrivateIp(string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out var ip))
                return true;

            return IPAddress.IsLoopback(ip) ||
                ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
            (
               ip.ToString().StartsWith("10.") ||
               ip.ToString().StartsWith("192.168.") ||
               ip.ToString().StartsWith("172.16.") ||
               ip.ToString().StartsWith("172.17.") ||
               ip.ToString().StartsWith("172.18.") ||
               ip.ToString().StartsWith("172.19.") ||
               ip.ToString().StartsWith("172.2")
           );
        }




    }
}
