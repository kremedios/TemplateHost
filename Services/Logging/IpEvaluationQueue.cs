using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Host.Services.Logging;


/**
What TryQueue(ip) does

For a new IP:

TryQueue("123.45.67.89")
        ↓
Is it already pending?
        ↓ No
Add it to _pending
        ↓
Put IP into _queue
        ↓
return true

If 20 requests from the same unknown IP arrive before the background evaluator processes it:

Request 1 → TryQueue → true  → queued
Request 2 → TryQueue → false
Request 3 → TryQueue → false
Request 4 → TryQueue → false
...
Request 20 → TryQueue → false

So you don't get 20 evaluations of the same IP.

Once the background evaluator finishes, it will call:

EvaluationComplete(ip);

That removes the IP from _pending, allowing that IP to be queued again if necessary.

One important point

_queue and _pending have different jobs:

_queue
   ↓
"What IPs are waiting to be evaluated?"

_pending
   ↓
"Which IPs have already been put into the queue
 and haven't finished evaluation?"

This is a good fit for your goal of keeping the request middleware extremely lightweight.

***We want one queue for the entire application.***

                            IpEvaluationQueue
                                    |
                                    |
            ________________________|________________________
            |                                               |
        Middleware                                  BackgroundService
            |                                               |
        TryQueue(ip)                                ReadAsync()

The next step would be registering this queue as a singleton in Program.cs, because both the middleware and the background service need to use the same queue instance.
*/

/**
IpEvaluationQueue is part of the application infrastructure. It contains:

- Channel<string>
- ConcurrentDictionary
- queueing behavior
- tracking pending evaluations
- methods for consuming the queue

It isn't a data contract that your RCLs need to know about.
*/


public class IpEvaluationQueue
{
    private readonly Channel<IpEvaluationRequest> _queue =
        Channel.CreateUnbounded<IpEvaluationRequest>();

    private readonly ConcurrentDictionary<string, byte> _pending = new();

    public bool TryQueue(string ip, string userAgent)
    {
        // Don't queue the same IP more than once
        // while it is awaiting evaluation.
        if (!_pending.TryAdd(ip, 0))
            return false;

        var request = new IpEvaluationRequest(ip, userAgent);

        if (_queue.Writer.TryWrite(request))
            return true;

        // Allow the IP to be queued again if writing failed.
        _pending.TryRemove(ip, out _);

        return false;
    }

    public async ValueTask<IpEvaluationRequest> ReadAsync(
        CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }

    public void EvaluationComplete(string ip)
    {
        _pending.TryRemove(ip, out _);
    }
}

public record IpEvaluationRequest(string Ip, string UserAgent);