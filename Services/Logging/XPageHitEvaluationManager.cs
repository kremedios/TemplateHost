
/**using System.Net.Http;
using System.Net.Http.Json;
using AppContractsSCO.Models.Common;
using Host.Models.Logging;

namespace Host.Services.Logging;
/**
Re using IpAPiRateLimiter in Check #2 in EvaluatePageHit(.) method:

What this gives you in practice:

Under normal/light traffic: essentially invisible — WaitAsync() returns 
immediately, no noticeable delay to page-hit processing.
Under heavy traffic (more than 45 hits/minute needing a reputation check): 
    EvaluatePageHit calls simply take a bit longer to complete, queuing up in 
    order, rather than any hit being dropped, blocked incorrectly, or the 
    ip-api.com service rejecting/banning you for exceeding its limit.

Since it's a singleton shared across the whole app, all callers 
(not just this one method, if you ever add more ip-api.com calls 
elsewhere) correctly share the same 45/minute budget.

*/

/**
public class PageHitEvaluationManager
{
    private readonly HttpClient _client;
    private readonly IHttpContextAccessor _http;  
    private readonly IpApiRateLimiter _rateLimiter;
    private readonly BotDetector _botDetector;


    public PageHitEvaluationManager(HttpClient client,
                                    IpApiRateLimiter rateLimiter,
                                    BotDetector botDetector,
                                    IHttpContextAccessor httpContextAccessor)
    {
        _client = client;
        _rateLimiter = rateLimiter;
        _botDetector = botDetector;
        _http = httpContextAccessor;
    }

    /**
    Using IpStore as reference for IP addresses and bots and associated entities 
    we return the action that guides the execution of the program, eg, blocking
    access or not, and whether the hit should be registered.  Namely, we do
    want bots to examine, but we don't want to register their hits as it doesn't
    reflect human hits.

    Return one of:
    - PageHitEvaluation.AllowAndRegister
    - PageHitEvaluation.AllowAndDoNotRegister
    - PageHitEvaluation.Block
    */
    /**
    public PageHitEvaluation GetEvaluation(string ipAddress)
    {
        // Determine if IpAddress is already in IpStore, i.e., if hit comes from non-human, e.g., bot
        var ipRecord = IpStore.Get(ipAddress);
        if (ipRecord is null)
            return PageHitEvaluation.AllowAndRegister; // IpAddress is not in bot file IpStore

        // At this point ipRecord exists in IpStore
        var accessIsBlocked = ipRecord.AccessIsBlocked;
        var hitIsToBeRegistered = ipRecord.HitIsToBeRegistered;

        if (accessIsBlocked)
            return PageHitEvaluation.Block;

        if (hitIsToBeRegistered)
            return PageHitEvaluation.AllowAndRegister;

        return PageHitEvaluation.AllowAndDoNotRegister;
    }



    /**
    Evaluate parameters contained in the specified PageHit object and provide value to populate the
    IpStore object in memory. Later these data should be written to the persistent
    store IpStore.json.

    This would be invoked in a method like HitStore... It would be separate from 
    method GetEvaluation() above.

    This should be called asynchronously.

    RETURNS
    - PageHitEvaluation enum value:
        - PageHitEvaluation.AllowAndDoNotRegister
        - PageHitEvaluation.AllowAndRegister
        - PageHitEvaluation.

    This method can be used for, but not limited to, populating IpStore,
    which is the reference source for execution actions for any particular
    IP.
    */

/**    public async Task<PageHitEvaluation> EvaluatePageHit(string area, string pageName)
    {
        //Get http context
        var httpContext = _http.HttpContext;

        //Get IP address (localhost-safe)
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (ip == "::1") ip = "127.0.0.1";

        //Get http request
        var request = httpContext.Request;

        //Get User-Agent
        var userAgent = request.Headers["User-Agent"].ToString();

        //At this point we have: IP and User-Agent

        //============================================
        //Check #1
        //if (LooksLikeBot(userAgent))
        //    return PageHitEvaluation.AllowAndDoNotRegister;

        // 1. Harmful/malicious bots — block outright
        if (_botDetector.IsHarmfulBot(userAgent))
            return PageHitEvaluation.Block;

        // 2. Known, legitimate bots — allow through, but don't count as a human hit
        if (_botDetector.IsKnownBot(userAgent))
            return PageHitEvaluation.AllowAndDoNotRegister;
        //============================================


        //============================================
        //Check #2
        // 3. Unknown/unrecognized UA — fall through to IP reputation check

        /**
        This check uses IpApiRateLimiter class that throttles/limits requests to
        ip-api.com in order to evaluate the give IP, eg, for bots. It is configured
        to limit requests to ip-api.com to 45 requests per minute, which thereafter
        would cause issues with ip-api.com. If the number of requests reaches this
        threshold, execution will pause and will resume some time afterwards.

        The method enclosing this code, EvaluatePageHit(.), is async so it's
        perfectly fine to use the rate limiter IpApiRateLimiter.

        In other words IpApiRateLimiter gives you in practice:

        - Under normal/light traffic: essentially invisible — WaitAsync() returns immediately, 
            no noticeable delay to page-hit processing.

        - Under heavy traffic (more than 45 hits/minute needing a reputation check): 
            EvaluatePageHit calls simply take a bit longer to complete, queuing up in order, 
            rather than any hit being dropped, blocked incorrectly, or the ip-api.com service 
            rejecting/banning you for exceeding its limit.

        - Since it's a singleton shared across the whole app, all callers (not just this 
            one method, if you ever add more ip-api.com calls elsewhere) correctly share 
            the same 45/minute budget.
        */
        

        // Wait for a permit before calling ip-api.com — this pauses (asynchronously,
        // not blocking a thread) until a slot within the 45/minute limit opens up.

/**        var acquired = await _rateLimiter.WaitAsync();
        if (!acquired)
        {
            // Queue was full — too many pending lookups. Fail safely.
            return PageHitEvaluation.AllowAndRegister;
        }

        var url = $"http://ip-api.com/json/{ip}?fields=isp,org,as,hosting,proxy,status";


        try
        {
            var result = await _client.GetFromJsonAsync<IpReputationResult>(url);
            if (result is not null && (result.Hosting || result.Proxy))
                return PageHitEvaluation.AllowAndDoNotRegister;

            return PageHitEvaluation.AllowAndRegister;
        }
        catch
        {
            return PageHitEvaluation.AllowAndRegister; // fail safely — treat lookup failure as "not bot"
        }
        //============================================
    }






}
*/