using Microsoft.Extensions.Hosting;

using System.Net;
using System.Net.Sockets;

using AppContractsSCO.Models.Common;

using Host.Services.Geo;

namespace Host.Services.Logging;

/// <summary>
/// This service will run in the background and take IP addresses 
/// from the queue for evaluation, without making incoming web requests wait.
/// 
/// Here is the sequence in more detail:
///
/// 1. ASP.NET Core starts your application.
///
/// 2. The host starts IpEvaluationBackgroundService because you registered it with AddHostedService.
///
/// 3. Its ExecuteAsync() method begins running and waits for queued requests.
///
/// 4. Your middleware receives an HTTP request and queues an IP evaluation when appropriate.
///
/// 5. The background service retrieves that queued request and calls EvaluateIpAsync(request).
///
/// 6. The evaluation determines the classification and updates IpStore.
///
/// 7. The background service can wait for work without blocking the thread that handles the incoming HTTP request.
///
/// One important distinction: AddHostedService starts the background service; it does not directly 
/// call EvaluateIpAsync(). The call happens inside the background service's queue-processing loop.
///
/// </summary>
public class IpEvaluationBackgroundService : BackgroundService
{
    private readonly ILogger<IpEvaluationBackgroundService> _logger;
    private readonly IpEvaluationQueue _queue;
    private readonly string _ipStorePath;
    private readonly GeoLookupService _geoLookupService;

    //public IpEvaluationBackgroundService(IpEvaluationQueue queue)
    //{
    //    _queue = queue;
    //}

    public IpEvaluationBackgroundService(
                IpEvaluationQueue queue,
                IHostEnvironment environment,
                GeoLookupService geoLookupService,
                ILogger<IpEvaluationBackgroundService> logger)
    {
        _queue = queue;
        _geoLookupService = geoLookupService;

        _ipStorePath = Path.Combine(
            environment.ContentRootPath,
            "Services",
            "Logging",
            "IpStore.json");

        _logger = logger;
    }







    protected override async Task ExecuteAsync(
                                                CancellationToken stoppingToken)
    ///
    /// ExecuteAsync() runs automatically when the application starts. It
    /// waits for queued requests.
    ///
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ///
            /// ReadAsync() waits for an IP to arrive in the queue. It doesn't repeatedly poll the queue.
            /// 
            //string ip = await _queue.ReadAsync(stoppingToken);
            IpEvaluationRequest request =
                    await _queue.ReadAsync(stoppingToken);

            try
            {
                ///
                /// EvaluateIpAsync(ip) is where we'll eventually determine whether the IP is Human, BotCrawler, or Blocked
                /// 
                //await EvaluateIpAsync(request.Ip,
                //                      request.UserAgent); 
                await EvaluateIpAsync(
                                        request.Ip,
                                        request.UserAgent,
                                        stoppingToken);
            }
            catch (Exception ex)
            {
                // Log the exception.
                    _logger.LogError(
                                ex,
                                "Error evaluating IP address {Ip}.",
                                request.Ip);
            }
            finally
            {
                ///
                /// finally removes the IP from the pending set even if evaluation throws an exception.
                /// 
                _queue.EvaluationComplete(request.Ip);
            }
        }
    }




    private async Task EvaluateIpAsync(string ip, 
                                       string userAgent, 
                                       CancellationToken cancellationToken)
    {
        /////////////////////////////////////////
        /// 1. Look up the hostname--DNS lookup
        /**
         string? hostName = null;

         try
            {
             var hostEntry = await System.Net.Dns.GetHostEntryAsync(ip);
             hostName = hostEntry.HostName;
            }
        catch
        {
            // No reverse DNS name found.
        }
        */

        string? hostName = null;

        try
        {
            if (IPAddress.TryParse(ip, out var address))
            {
                var hostEntry = await Dns.GetHostEntryAsync(address);

                var candidateHostName = hostEntry.HostName;

                if (!string.IsNullOrWhiteSpace(candidateHostName))
                {
                    var resolvedAddresses =
                        await Dns.GetHostAddressesAsync(candidateHostName);

                    if (resolvedAddresses.Any(
                            resolvedAddress => resolvedAddress.Equals(address)))
                    {
                        hostName = candidateHostName;
                    }
                }
            }
        }
        catch (SocketException)
        {
            // DNS lookup failed. This does not mean the IP is hostile.
        }
        catch (ArgumentException)
        {
            // Invalid input for a DNS lookup.
        }



        /////////////////////////////////////////
        /// 2. Look up the geographic location--GeoIP lookup
        GeoLocation location = _geoLookupService.Lookup(ip);


            Console.WriteLine(
        $"IP EVALUATION: {ip} | " +
        $"Host: {hostName ?? "(none)"} | " +
        $"Country: {location.Country} | " +
        $"City: {location.City} | " +
        $"User-Agent: {userAgent}");
        
        
        
        //RequestType requestType;
       



        /////////////////////////////////////////
        // UserAgent evaluation
        // Classification logic.
    

      
        RequestType requestType;

        if (IsVerifiedSearchCrawler(userAgent, hostName))
        {
            requestType = RequestType.BotCrawler;
        }
        else
        {
            requestType =
                ClassifyUserAgent(userAgent) ?? RequestType.Human;
        }








        /////////////////////////////////////////
        //Create IpRecord
        var record = new IpRecord
        {
            Ip = ip,
            Country = location.Country,
            RequestType = requestType
        };



        /////////////////////////////////////////
        // Store the result so that the in-memory dictionary is updated
        IpStore.AddOrUpdate(record);



        /////////////////////////////////////////
        //Persist the IpStore to json file
        //NB  We are doing this in the background service
        IpStore.Save(_ipStorePath);

        await Task.CompletedTask;
    }







    private static RequestType? ClassifyUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        string[] crawlerMarkers =
        [
            "bot",
            "crawler",
            "spider",
            "slurp",
            "bingpreview",
            "facebookexternalhit",
            "twitterbot",
            "linkedinbot",
            "googleother"
        ];

        if (crawlerMarkers.Any(marker =>
            userAgent.Contains(
                marker,
                StringComparison.OrdinalIgnoreCase)))
        {
            return RequestType.BotCrawler;
        }

        bool looksLikeBrowser =
            userAgent.Contains("Mozilla/", StringComparison.OrdinalIgnoreCase) &&
            (
                userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase) ||
                userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase) ||
                userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase) ||
                userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase)
            );

        if (looksLikeBrowser)
        {
            return RequestType.Human;
        }

        return null;
    }








    private static bool IsVerifiedSearchCrawler(
        string userAgent,
        string? verifiedHostName)
    {
        if (string.IsNullOrWhiteSpace(userAgent) ||
            string.IsNullOrWhiteSpace(verifiedHostName))
        {
            return false;
        }

        string host = verifiedHostName.TrimEnd('.');

        bool claimsGooglebot =
            userAgent.Contains(
                "Googlebot",
                StringComparison.OrdinalIgnoreCase);

        bool claimsBingbot =
            userAgent.Contains(
                "bingbot",
                StringComparison.OrdinalIgnoreCase);

        bool verifiedGoogleHost =
            host.EndsWith(
                ".googlebot.com",
                StringComparison.OrdinalIgnoreCase) ||
            host.Equals(
                "googlebot.com",
                StringComparison.OrdinalIgnoreCase);

        bool verifiedBingHost =
            host.EndsWith(
                ".search.msn.com",
                StringComparison.OrdinalIgnoreCase);

        return (claimsGooglebot && verifiedGoogleHost) ||
            (claimsBingbot && verifiedBingHost);
    }






}