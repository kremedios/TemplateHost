
namespace Host.Services.Logging;

/// <summary>
/// Example:
/// PageRequestRateStore.IsWithinLimit(
///     ip, 100, TimeSpan.FromMinutes(5));
/// </summary>
public static class PageRequestRateStore
{
    private static readonly Dictionary<string, Queue<DateTime>> _requests = new();

    private static readonly object _lock = new();

    /// <summary>
    /// Allows up to maximumRequests per IP within the rolling time window.
    /// Only allowed requests are recorded.
    /// </summary>
    public static bool IsWithinLimit(
        string ip,
        int maximumRequests,
        TimeSpan timeWindow)
    {
        var now = DateTime.UtcNow;
        var cutoff = now - timeWindow;

        lock (_lock)
        {
            if (!_requests.TryGetValue(ip, out var times))
            {
                times = new Queue<DateTime>();
                _requests[ip] = times;
            }

            // Remove expired requests from the front of the queue.
            while (times.Count > 0 && times.Peek() <= cutoff)
            {
                times.Dequeue();
            }

            // Allow requests 1 through maximumRequests.
            if (times.Count >= maximumRequests)
            {
                return false;
            }

            // Record only allowed requests.
            times.Enqueue(now);

            return true;
        }
    }

    /// <summary>
    /// Removes expired timestamps and inactive IP entries.
    /// </summary>
    public static void CleanupInactiveIps(TimeSpan timeWindow)
    {
        var cutoff = DateTime.UtcNow - timeWindow;

        lock (_lock)
        {
            var inactiveIps = new List<string>();

            foreach (var entry in _requests)
            {
                var times = entry.Value;

                while (times.Count > 0 && times.Peek() <= cutoff)
                {
                    times.Dequeue();
                }

                if (times.Count == 0)
                {
                    inactiveIps.Add(entry.Key);
                }
            }

            foreach (var ip in inactiveIps)
            {
                _requests.Remove(ip);
            }
        }
    }
}