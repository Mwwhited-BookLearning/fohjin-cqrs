using Microsoft.Extensions.Logging;

namespace Fohjin.DDD.Common;

public class SystemTimer(
    ILogger<SystemTimer> log) : ISystemTimer, IDisposable
{
    private readonly List<Task> _timers = new();
    private readonly ILogger _log = log;

    public void Dispose() =>
        Task.WaitAll([.. _timers]);

    public void Trigger(Func<Task> value, int @in)
    {
        _log.LogInformation("Schedule Timer: {value} ({in})", value, @in);

        // Trigger is called from the UI thread (a button click, a saved form), so capture its
        // SynchronizationContext now - Task.Run below drops onto the thread pool with none, and
        // running `value` there instead would set WinForms-bound properties off the UI thread.
        var uiContext = SynchronizationContext.Current;

        _timers.Add(Task.Run(async () =>
        {
            try
            {
                await Task.Delay(@in);
                _log.LogInformation("Triggered Timer: {value} ({in})", value, @in);

                if (uiContext == null)
                {
                    await value();
                    return;
                }

                var completion = new TaskCompletionSource();
                uiContext.Post(async _ =>
                {
                    try
                    {
                        await value();
                        completion.SetResult();
                    }
                    catch (Exception ex)
                    {
                        completion.SetException(ex);
                    }
                }, null);
                await completion.Task;
            }
            catch (Exception ex)
            {
                // Otherwise this sits unobserved in _timers until Dispose() calls Task.WaitAll,
                // which is long after the failure actually happened and doesn't log anything itself.
                _log.LogError(ex, "Timer callback {value} ({in}) failed", value, @in);
            }
        }));
    }
}
