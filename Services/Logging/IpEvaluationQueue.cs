using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Host.Services.Logging;


/**
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

The next step would be registering this queue as a singleton in Program.cs, because both the middleware and the background service need to use the same queue instance.
*/



public class IpEvaluationQueue
{
    private readonly Channel<string> _queue =
        Channel.CreateUnbounded<string>();

    private readonly ConcurrentDictionary<string, byte> _pending = new();

    public bool TryQueue(string ip)
    {
        // Don't queue the same IP more than once
        // while it is waiting to be evaluated.
        if (!_pending.TryAdd(ip, 0))
            return false;

        // Put the IP on the queue.
        if (_queue.Writer.TryWrite(ip))
            return true;

        // If the write failed, allow the IP to be queued again later.
        _pending.TryRemove(ip, out _);

        return false;
    }

    public async ValueTask<string> ReadAsync(
        CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }

    public void EvaluationComplete(string ip)
    {
        _pending.TryRemove(ip, out _);
    }
}