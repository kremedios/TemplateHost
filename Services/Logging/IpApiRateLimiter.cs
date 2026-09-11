using System.Threading.RateLimiting;
/**
Overall purpose

IpApiRateLimiter is a thin wrapper around .NET's built-in SlidingWindowRateLimiter, 
dedicated specifically to controlling how often your app is allowed to call ip-api.com. 
Since that service caps you at 45 requests/minute on the free tier, this class ensures 
your app never exceeds that — instead of failing or getting blocked, calls simply wait 
their turn until a slot opens up.

How it works internally

It wraps a SlidingWindowRateLimiter configured for 45 permits per 1-minute window, 
split into 6 smaller sub-segments — this smooths out bursts (avoids the "45 requests 
in the last second of one minute + 45 in the first second of the next" problem that 
a simpler fixed-window approach could allow).
Its one public method, WaitAsync(), asks the limiter for a permit. If one's available, 
it returns almost instantly. If not, the await pauses (without blocking a thread — other 
requests in your app keep being handled normally in the meantime) until a permit frees up.
With QueueLimit set high enough (or int.MaxValue), it effectively never refuses — it just 
waits as long as necessary, which matches what you said you want: no dropped/allowed-through 
hits, just a delay until it's safe to call ip-api.com.
*/
public class IpApiRateLimiter
{
    private readonly RateLimiter _limiter;

    public IpApiRateLimiter()
    {
        _limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 45,                          // max 45 requests
            Window = TimeSpan.FromMinutes(1),           // per 1 minute
            SegmentsPerWindow = 6,                       // smooths bursts within the window
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = int.MaxValue                    // effectively never rejects — always queues and waits
        });
    }

    public async Task<bool> WaitAsync(CancellationToken cancellationToken = default)
    {
        using var lease = await _limiter.AcquireAsync(1, cancellationToken);
        return lease.IsAcquired;
    }
}