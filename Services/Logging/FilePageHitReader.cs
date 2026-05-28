using AppContractsSCO.Services.Logging;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Host.Controllers.Logging;


public class FilePageHitReader : IPageHitService
{
    private readonly IWebHostEnvironment _env;

    public FilePageHitReader(IWebHostEnvironment env)
    {
        _env = env;
    }

    public Task<IEnumerable<string[]>> GetPageHitsAsync(string area, string pageName)
    {
        var logHelper = new LogHelper(_env, area, pageName);
        string logFile = logHelper.GetLogFile(pageName);

        if (!File.Exists(logFile))
            return Task.FromResult(Enumerable.Empty<string[]>());

        var entries = File.ReadAllLines(logFile)
            .Where(l => !string.IsNullOrWhiteSpace(l) && l.Split('|').Length >= 4)
            .Select(l => l.Split('|'));

        return Task.FromResult(entries);
    }






public Task LogPageHitAsync(string area, string pageName)
{
    throw new NotImplementedException();
}

public int GetPageHitCount(string area, string pageName)
{
    throw new NotImplementedException();
}

public string? GetLastHitDatetime(string area, string pageName)
{
    throw new NotImplementedException();
}





}