using System.Net.Http;
using System.Net.Http.Json;
using AppContractsSCO.Models.Common;
using Host.Models.Logging;

namespace Host.Services.Logging;

public class PageHitEvaluationManager
{
    private readonly HttpClient _client;
     private readonly IpApiRateLimiter _rateLimiter;


    public PageHitEvaluationManager(HttpClient client,
                                    IpApiRateLimiter rateLimiter)
    {
        _client = client;
        _rateLimiter = rateLimiter;
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
    */
    public async Task<PageHitEvaluation> EvaluatePageHit(PageHit pageHit)
    {
        //============================================
        //Check #1
        var userAgent = pageHit.UserAgent;
        if (LooksLikeBot(userAgent))
            return PageHitEvaluation.AllowAndDoNotRegister;
        //============================================


        //============================================
        //Check #2
        var ip = pageHit.IpAddress;

        // Wait for a permit before calling ip-api.com — this pauses (asynchronously,
        // not blocking a thread) until a slot within the 45/minute limit opens up.
        var acquired = await _rateLimiter.WaitAsync();
        if (!acquired)
        {
            // Queue was full — too many pending lookups. Fail safely.
            return PageHitEvaluation.AllowAndRegister;
        }

        var url = $"http://ip-api.com/json/{ip}?fields=isp,org,as,hosting,proxy,status";




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

    private static readonly string[] _botSignatures = new[]
    {
        "bot", "crawl", "spider", "scrape", "slurp",
        "curl", "wget", "python-requests", "python-urllib",
        "go-http-client", "okhttp", "postmanruntime", "axios",
        "headlesschrome", "phantomjs", "puppeteer", "playwright", "selenium",
        "nmap", "nikto", "sqlmap", "masscan", "zgrab"
    };

    private static bool LooksLikeBot(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return true;

        return _botSignatures.Any(sig =>
            userAgent.Contains(sig, StringComparison.OrdinalIgnoreCase));
    }
}