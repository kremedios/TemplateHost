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

namespace Host.Services.Logging
{
    public class PageHitService : IPageHitService
    {
        //private readonly BotDetector _botDetector;
        private readonly GeoLookupService _geoService;
        private readonly IHttpContextAccessor _http;        
        private readonly IWebHostEnvironment _env;

        public PageHitService(GeoLookupService geoService,
                              IHttpContextAccessor http,
                              IWebHostEnvironment env)
        {
            _geoService = geoService;
            _http = http;
            _env = env;
            //_botDetector = botDetector;
        }

        /// <summary>
        /// Logs a page hit, restricted by area-based role.
        /// </summary>
        //public Task LogPageHitAsync(string area, string pageName, HttpContext httpContext)
        public Task LogPageHitAsync(string area, string pageName)        
        {
            var httpContext = _http.HttpContext;
            var user = httpContext.User;

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






            // User-Agent
            var userAgent = request.Headers["User-Agent"].ToString();


            // Bot Check -- skip logging entirely for known bots
            //if (_botDetector.IsKnownBot(userAgent))
            //{
            //    return Task.CompletedTask;
            //}



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



            //== Evaluate the IP =======
            IpEvaluation IpAction = EvaluateIp(hit.IpAddress);
            //=========================

            if (IpAction == IpEvaluation.AllowAndDoNotRegister)
                return Task.CompletedTask; //Exit method without logging anything
            



            // ===== FILE LOGGING  - begin =====
            var year = hitTime.Year;
            var baseDir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "private",
                area,
                pageName,
                "logs"
            );

            //Console.WriteLine($"@@@PageHitService: baseDir = {baseDir}");
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
             // ===== FILE LOGGING  - end =====




            return Task.CompletedTask;
        }






        /**
        Using IpStore as reference for IP addresses and bots and associated entities 
        we return the action that guides the execution of the program, eg, blocking
        access or not, and whether the hit should be registered.  Namely, we don't
        want bots to examine, but we don't want to register their hits as it doesn't
        reflect human hits.

        Return one of:
        - IpEvaluation.AllowAndRegister
        - IpEvaluation.AllowAndDoNotRegister
        - IpEvaluation.Block
        */
        private IpEvaluation EvaluateIp(string IpAddress)
        {
            //Determine if IpAddress is already in IpStore, i.e., if hit comes from non-human, e.g., bot
            var IpRecord = IpStore.Get(IpAddress);
            if (IpRecord is null)
                return(IpEvaluation.AllowAndRegister);//IpAddress is not in bot file IpStore

            var accessIsBlocked = IpRecord.AccessIsBlocked;
            var hitIsToBeRegistered = IpRecord.HitIsToBeRegistered;

            IpEvaluation returnVal = IpEvaluation.AllowAndRegister;//default
            if (!accessIsBlocked && !hitIsToBeRegistered)
                returnVal = IpEvaluation.AllowAndDoNotRegister;
            else if (!accessIsBlocked && hitIsToBeRegistered )
                returnVal = IpEvaluation.AllowAndRegister;
            else if (accessIsBlocked)
                returnVal = IpEvaluation.Block;
            else
                returnVal = IpEvaluation.AllowAndDoNotRegister;

            return returnVal;
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
            IpEvaluation ipAction = EvaluateIp(ip);

            if (ipAction == IpEvaluation.Block) 
                returnVal = true;

            return returnVal;
        }





    }
}
