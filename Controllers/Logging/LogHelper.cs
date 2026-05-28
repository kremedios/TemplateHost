using Microsoft.AspNetCore.Hosting;

namespace Host.Controllers.Logging;
public class LogHelper
{
    private string _logsDir;

    public LogHelper(IWebHostEnvironment env, string area, string pageName)
    {
        _logsDir = Path.Combine(
            env.ContentRootPath,   // always points to the root of the host app (eg, TemplateHost/)
            "private",
            area,
            pageName,
            "logs"
        );

        Directory.CreateDirectory(_logsDir); // ensures it exists
    }

    public string GetLogFile(string pageName)
    {
        return Path.Combine(_logsDir, $"{pageName}-{DateTime.Now.Year}.log");
    }
}