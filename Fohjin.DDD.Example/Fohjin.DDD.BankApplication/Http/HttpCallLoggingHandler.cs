using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Fohjin.DDD.BankApplication.Http;

// Phase 7 (docs/11-migration-plan.md): the monitoring pane's log half (MonitoringLoggerProvider)
// used to capture in-process bus/command-handler/event-store logging - none of that runs in
// this process anymore now that every operation is an HTTP call to Fohjin.DDD.WebApi. This
// handler logs each outgoing call instead, through the same ILogger/MonitoringLoggerProvider
// pipeline MonitoringPresenter already subscribes to, so the pane keeps showing something
// meaningful without MonitoringPresenter itself needing to change.
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
