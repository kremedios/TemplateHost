namespace Host.Services.Logging;

/// <summary>
/// Example of this being used:
/// 
/// PageRequestRateStore.IsWithinLimit(
///    ip,
///    100,
///    TimeSpan.FromMinutes(5));
/// 
/// </summary>

public static class PageRequestRateStore
{
    private static readonly Dictionary<string, List<DateTime>> _requests = new();
    private static readonly object _lock = new();

    /**
    One subtle but important point:

    This method records the request only when it is allowed.

    So if request #101 is rejected, we don't keep adding rejected requests to the list. 
    That prevents an already-over-limit IP from making the in-memory list grow indefinitely.
    */
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
                times = new List<DateTime>();
                _requests[ip] = times;
            }

            // Remove requests that are outside the time window.
            times.RemoveAll(t => t < cutoff);

            // The current request has not been recorded yet.
            // Therefore, maximumRequests means:
            // allow requests 1 through maximumRequests.
            if (times.Count >= maximumRequests)
            {
                return false;
            }

            // Record this request.
            times.Add(now);

            return true;
        }
    }
}

