using AppContractsSCO.Models.Common;

namespace Host.Services.Logging;

public static class HitStore
{
   
    private static readonly List<HitRecord> _hits = new();
    private static readonly object _lock = new();

    public static void Add(string ip)
    {
        lock (_lock)
        {
             //This List is thread-safe
            _hits.Add(new HitRecord
            {
                Ip = ip,
                HitTime = DateTime.UtcNow
            });
        }
    }




    public static bool IsWithinLimit(string ip,
                                     int maximumHits,
                                     TimeSpan timeWindow)
    {
        var cutoff = DateTime.UtcNow - timeWindow;

        lock (_lock)
        {
            int hitCount = _hits.Count(h =>
                h.Ip == ip &&
                h.HitTime >= cutoff);

            return hitCount <= maximumHits;
        }
    }



}