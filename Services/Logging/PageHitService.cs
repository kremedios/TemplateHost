using Microsoft.AspNetCore.Http;
//using Plumspaces.Services.Geo;
using System;
using System.IO;
using System.Linq;

using System.Security;
using System.Threading.Tasks;

using AppContractsSCO.Services.Logging;
using AppContractsSCO.Models.Common;

using Host.Models.Logging;
using Host.Models.Datetime;
using Host.Services.Geo;
using Host.Controllers.Logging;
using System.Net;
using Host.Services.Logging;

namespace Host.Services.Logging
{
    public class PageHitService : IPageHitService
    {
        //private readonly BotDetector _botDetector;
        private readonly GeoLookupService _geoService;
        private readonly IHttpContextAccessor _http;        
        private readonly IWebHostEnvironment _env;
        private readonly PageHitEvaluationManager _pageHitEvaluationManager;

        public PageHitService(GeoLookupService geoService,
                              IHttpContextAccessor http,
                              IWebHostEnvironment env,
                              PageHitEvaluationManager pageHitEvaluationManager)
        {
            _geoService = geoService;
            _http = http;
            _env = env;
            //_botDetector = botDetector;
            _pageHitEvaluationManager = pageHitEvaluationManager;
        }

        /// <summary>
        /// Logs a page hit, restricted by area-based role.
        /// This method only registers the web page hit if it's from a human.
        /// </summary>
        //public Task LogPageHitAsync(string area, string pageName, HttpContext httpContext)
        public async Task LogPageHitAsync(string area, string pageName)        
        {
            var httpContext = _http.HttpContext;


            //There is Middleware located in Program.cs, which identifies 
            //the type for *every* incoming IP request. 
            //
            //Interpret HttpContext as the *container representing the current request*
            //
            //NB Middleware already has handled IP addresses that had been deemed hostile.
            //   Therefore, there are only 2 possible cases:
            //   - Hit is from human
            //   - Hit is from bot
            //
            //   We only count human hits, but still allow bot/crawler to access web page
            var requestType =
                httpContext.Items[RequestKeys.RequestType] is RequestType tempVariable
                    ? tempVariable
                    : RequestType.Human;

            if (requestType == RequestType.BotCrawler)
            {
                //This is a bot/crawler hit
                //We do not register any info
                return;
            }





            //================================
            //This is a human page hit - begin
            //================================



            //var user = httpContext.User;

            // Only allow users with the correct role for the given area
            // bool allowed = (string.Equals(area, "Rentals", StringComparison.OrdinalIgnoreCase) && user.IsInRole("Admin:Rentals"))
            //             || (string.Equals(area, "juderemedios", StringComparison.OrdinalIgnoreCase) && user.IsInRole("Admin:Juderemedios"))
            //             || (string.Equals(area, "remedios", StringComparison.OrdinalIgnoreCase) && user.IsInRole("Admin:Remedios"));

            // if (!allowed)
            //     throw new SecurityException($"User '{user.Identity?.Name}' is not authorized to access area '{area}'.");

            var request = httpContext.Request;

            // IP address (localhost-safe)
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (ip == "::1") ip = "127.0.0.1";


            //== TEMP ONLY - USE ONLY AT URL PRE-LAUNCH begin =========================================================
            //== Collect bot IP's
            //=== ***Only use*** at prelaunch of URL publishing collect bot IP addresses - begin
            InitialBotIpCollector.StoreIp(ip, "./Services/Logging/IpStore.json");
            //=== ***Only use*** at prelaunch of URL publishing collect bot IP addresses - end
            //== TEMP ONLY - USE ONLY AT URL PRE-LAUNCH end ===========================================================




            /**
            //=== Process the IP ===
            //Add IP for temporary storage and evaluation
            HitStore.Add(ip);


            //Determine if too many hits per time period ==> unfriendly bot (not just a crawler)
            int maxAllowableHits = 10;
            int nbrMinutesInTimeSpanWindow = 1;
            bool allowable = HitStore.IsWithinLimit(ip,
                                                    maxAllowableHits,
                                                    TimeSpan.FromMinutes(nbrMinutesInTimeSpanWindow));
            //======================

            **/
            





            // User-Agent
            var userAgent = request.Headers["User-Agent"].ToString();

            var device =
                userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ? "Mobile" :
                userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase) ? "Tablet" :
                "Desktop";

            var hitTime = TimeMgr.NowOnCentralTime();

            // Default location values
            string city = "local city";
            string state = "local state";
            string country = "local country";

            // Geo lookup (safe, never crash)
            try
            {
                if (ip != "127.0.0.1")
                {
                    var location = _geoService.Lookup(ip);
                    city = location.City;
                    state = location.State;
                    country = location.Country;
                }
            }
            catch
            {
                // swallow geo lookup failures
            }


            //Store IP and other relevant data in PageHit object (for convenience)
            var hit = new PageHit
            {
                PageName = pageName,
                IpAddress = ip,
                UserAgent = userAgent,
                Device = device,
                City = city,
                State = state,
                Country = country,
                HitTimeCentral = hitTime
            };



            /**
            Capture the webpage hit and evaluate it asynchronously; we wait for how to handle the hit
            */
         ////   var result = await _pageHitEvaluationManager.EvaluatePageHit(hit);




            //== Get the IP Evaluation previously stored in IpStore - begin ====================
            //PageHitEvaluation PageHitAction = PageHitEvaluationManager.GetEvaluation(hit.IpAddress);
            // PageHitEvaluation PageHitAction = _pageHitEvaluationManager.GetEvaluation(hit.IpAddress);
            //== Eet the IP Evaluation previously stored in IpStore - end ======================


            //Continue program execution without logging anything
            //if (PageHitAction == PageHitEvaluation.AllowAndDoNotRegister)
            //    return Task.CompletedTask;
                

 




            // ===== Do FILE LOGGING  - begin =====
            var year = hitTime.Year;
            var baseDir = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "private",
                    area,
                    pageName,
                    "logs"
            );


            Directory.CreateDirectory(baseDir);

            var logFile = Path.Combine(baseDir, $"{pageName}-{year}.log");
            var lastHitDatetimeFile = Path.Combine(baseDir, $"{pageName}-LastHitDatetime.log" );

            // Read existing lines safely
            int lineCount = 0;
            if (File.Exists(logFile))
            {
                lineCount = File.ReadAllLines(logFile).Length;
            }

            string lineNbr = (lineCount + 1).ToString() + ":";

            var line =
                    lineNbr + " " +
                    $"{hit.HitTimeCentral:MM-dd-yy HH:mm} | " +
                    $"{hit.IpAddress} | " +
                    $"{hit.City}, {hit.State}, {hit.Country} | " +
                    //$"{hit.Device} | " +
                    $"{hit.IpAddress}" + 
                    $"{hit.UserAgent}";

            ///////// try
            File.WriteAllText(lastHitDatetimeFile, $"{hit.HitTimeCentral:MM-dd-yy HH:mm}");
            /// end try



            File.AppendAllText(logFile, line + Environment.NewLine);
            // ===== Do FILE LOGGING  - end =======

            return;

            //================================
            //This is a human page hit - end
            //================================
        }














        /// <summary>
        /// Gets the number of hits for a given page in a specific area.
        /// </summary>
        public int GetPageHitCount(string area, string pageName)
        {
            var year = TimeMgr.NowOnCentralTime().Year;

            var logFile = Path.Combine(
                Directory.GetCurrentDirectory(),
                "private",
                area,
                pageName,
                "logs",
                $"{pageName}-{year}.log"
            );

            if (!File.Exists(logFile))
                return 0;

            return File.ReadLines(logFile)
                       .Count(line => !string.IsNullOrWhiteSpace(line));
        }

        public string GetLastHitDatetime(string area, string pageName)
        {
            var logFile = Path.Combine(
                Directory.GetCurrentDirectory(),
                "private",
                area,
                pageName,
                "logs",
                $"{pageName}-LastHitDatetime.log"
            );

            if (!File.Exists(logFile))
                return " ";  

            return File.ReadAllText(logFile);
        }




public Task<IEnumerable<string[]>> GetPageHitsAsync(string area, string pageName)
{
    var logHelper = new LogHelper(_env, area, pageName);
    string logFile = logHelper.GetLogFile(pageName);

    if (!System.IO.File.Exists(logFile))
        return Task.FromResult(Enumerable.Empty<string[]>());

    var entries = System.IO.File.ReadAllLines(logFile)
        .Where(l => !string.IsNullOrWhiteSpace(l) && l.Split('|').Length >= 4)
        .Select(l => l.Split('|'));

    return Task.FromResult(entries);
}






        /**
        Given an IP, decide whether or not to issue a bool as to whether to block, say access
        */
        public bool BlockAccess()
        {
            bool returnVal = false;
            var httpContext = _http.HttpContext;

            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            //Use IpStore as reference for non-human IP addresses
            //PageHitEvaluation ipAction = PageHitEvaluationManager.GetEvaluation(ip);//EvaluateIp(ip);
            PageHitEvaluation ipAction = _pageHitEvaluationManager.GetEvaluation(ip);//EvaluateIp(ip);

            if (ipAction == PageHitEvaluation.Block) 
                returnVal = true;

            return returnVal;
        }


        public BotDestiny HandleKnownBot()
        {
            var returnVal = BotDestiny.Redirect;
            //if bot is to be blocked


            //if bot is not blocked, then we direct to no image web page  
            return returnVal;
        }



    }
}
