using Microsoft.Extensions.Hosting;

using AppContractsSCO.Models.Common;

namespace Host.Services.Logging;

/// <summary>
/// This service will run in the background and take IP addresses 
/// from the queue for evaluation, without making incoming web requests wait.
/// </summary>
public class IpEvaluationBackgroundService : BackgroundService
{
    private readonly IpEvaluationQueue _queue;
    private readonly string _ipStorePath;

    //public IpEvaluationBackgroundService(IpEvaluationQueue queue)
    //{
    //    _queue = queue;
    //}

    public IpEvaluationBackgroundService(
                IpEvaluationQueue queue,
                IHostEnvironment environment)
    {
        _queue = queue;

        _ipStorePath = Path.Combine(
            environment.ContentRootPath,
            "Services",
            "Logging",
            "IpStore.json");
    }







    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    ///
    /// ExecuteAsync() runs automatically when the application starts.
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
                 //await EvaluateIpAsync(ip);
                //await EvaluateIpAsync(request.Ip, request.UserAgent);
                await EvaluateIpAsync(
                    request.Ip,
                    request.UserAgent); 
            }
            catch (Exception ex)
            {
                // TODO: Log the exception.
            }
            finally
            {
                ///
                /// finally removes the IP from the pending set even if evaluation throws an exception.
                /// 
                //_queue.EvaluationComplete(ip);
                _queue.EvaluationComplete(request.Ip);
            }
        }
    }




    private async Task EvaluateIpAsync(string ip, string userAgent)
    {
        RequestType requestType;
       

        // Temporary classification logic.
        // We will replace this with the actual evaluation rules.
        if (userAgent.Contains("bot", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("crawler", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("spider", StringComparison.OrdinalIgnoreCase))
        {
            requestType = RequestType.BotCrawler;
        }
        else
        {
            requestType = RequestType.Human;
        }


        var record = new IpRecord
        {
            Ip = ip,
            Country = "",
            RequestType = requestType
        };


//Console.WriteLine(
//    $"IP EVALUATION: {ip} | " +
//    $"User-Agent: {userAgent} | " +
//    $"Result: {requestType}" + DateTime.Now);


        IpStore.AddOrUpdate(record);

        //Persist the IpStore to json file
        //NB  We are doing this in the background service
        IpStore.Save(_ipStorePath);

        await Task.CompletedTask;
    }


}