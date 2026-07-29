using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Fohjin.DDD.DesktopClient;

// Every desktop client's monitoring/log pane (WinForms' MonitoringLoggerProvider, WPF's
// equivalent) captures this instead of in-process bus/command-handler logging, since every
// operation is an HTTP call to Fohjin.DDD.WebApi rather than something running in this process.
public class HttpCallLoggingHandler(ILogger<HttpCallLoggingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken);
        stopwatch.Stop();

        logger.LogInformation(
            "{Method} {Path} -> {StatusCode} ({ElapsedMs} ms)",
            request.Method,
            request.RequestUri?.PathAndQuery,
            (int)response.StatusCode,
            stopwatch.ElapsedMilliseconds);

        return response;
    }
}
