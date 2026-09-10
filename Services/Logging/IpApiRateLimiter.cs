using System.Threading.RateLimiting;

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