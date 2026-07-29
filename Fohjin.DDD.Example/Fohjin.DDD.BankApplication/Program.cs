using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Auth;
using Fohjin.DDD.BankApplication.Http;
using Fohjin.DDD.BankApplication.Presenters;
using Fohjin.DDD.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fohjin.DDD.BankApplication;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddIniFile("appsettings.ini", optional: true)
            .AddJsonFile("appsettings.json", optional: true)
            .AddXmlFile("appsettings.xml", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        var monitoringLoggerProvider = new MonitoringLoggerProvider();

        // Phase 7 (docs/11-migration-plan.md): every operation is now an HTTP call to
        // Fohjin.DDD.WebApi through the generated FohjinApiClient (Fohjin.DDD.ApiClient) instead
        // of an in-process call - no more AddBusServices()/AddEventStoreServices()/etc. wiring
        // the CQRS core directly, and no more BootStrapApplicationAsync()/SubscribeEventHandlers()
        // migrating/subscribing to a local SQLite event store that no longer exists in this
        // process. AuthorizationHandler attaches the token DesktopAuthService obtains;
        // HttpCallLoggingHandler feeds the monitoring pane's log half the same way in-process
        // bus/command-handler logging used to (see HttpCallLoggingHandler's own comment).
        var services = new ServiceCollection()
            .AddSingleton(monitoringLoggerProvider)
            .AddSingleton<ILoggerProvider>(monitoringLoggerProvider)
            .AddLogging(opt => opt.AddConsole().AddDebug()
#if DEBUG
                .SetMinimumLevel(LogLevel.Debug)
#else
                .SetMinimumLevel(LogLevel.Information)
#endif
                )
            .AddSingleton<IConfiguration>(configuration)
            .AddCommonServices()
            .AddBankApplicationServices()
            ;

        // AddHttpMessageHandler<T>() only wires T into the client pipeline via
        // services.GetRequiredService<T>() - it does NOT register T itself, so both handlers
        // need an explicit registration or resolving FohjinApiClient/EventStreamClient throws
        // "No service for type ... has been registered."
        services.AddTransient<AuthorizationHandler>();
        services.AddTransient<HttpCallLoggingHandler>();

        // Deliberately NOT services.AddHttpClient<DesktopAuthService>() - that registers it as
        // a typed client, which resolves a NEW DesktopAuthService instance every time (only the
        // underlying HttpMessageHandler is pooled/reused, not the wrapper). AuthorizationHandler
        // depends on reading back the SAME instance's AccessToken that Main sets via
        // LoginAsync() below, so DesktopAuthService must be a singleton - a fresh instance for
        // AuthorizationHandler always has AccessToken == null, silently sending every request
        // with no Authorization header (401s that look like an auth/config problem, not a DI
        // lifetime one).
        services.AddSingleton(sp => new DesktopAuthService(
            new HttpClient(),
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<ILogger<DesktopAuthService>>()));

        services.AddHttpClient<FohjinApiClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["WebApi:BaseUrl"]
                ?? throw new InvalidOperationException("WebApi:BaseUrl is not configured."));
        })
            .AddHttpMessageHandler<AuthorizationHandler>()
            .AddHttpMessageHandler<HttpCallLoggingHandler>();

        // No HttpCallLoggingHandler here - that handler logs once per SendAsync, which for a
        // long-lived SSE connection would only ever log the single initial "GET api/events"
        // call, and isn't worth the confusion of appearing to hang on a call that's actually a
        // deliberately-persistent stream.
        services.AddHttpClient<EventStreamClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["WebApi:BaseUrl"]
                ?? throw new InvalidOperationException("WebApi:BaseUrl is not configured."));
        })
            .AddHttpMessageHandler<AuthorizationHandler>();

        var serviceProvider = services.BuildServiceProvider();

        // Desktop OIDC login (docs/11-migration-plan.md Phase 7's system-browser + loopback
        // redirect decision) happens once, up front, before any window is shown - every
        // presenter's FohjinApiClient calls assume a signed-in AuthorizationHandler.AccessToken
        // is already in place by the time they run.
        await serviceProvider.GetRequiredService<DesktopAuthService>().LoginAsync();

        var clientSearchFormPresenter = serviceProvider.GetRequiredService<IClientSearchFormPresenter>();
        var monitoringPresenter = serviceProvider.GetRequiredService<IMonitoringPresenter>();
        Application.EnableVisualStyles();
        monitoringPresenter.Display();
        clientSearchFormPresenter.Display();

        // Both Display() calls above are async void, kicking off `await LoadDataAsync()` (a
        // real HTTP call) and returning immediately - nothing else here would otherwise keep
        // this STA thread's message loop pumping long enough for those continuations (and the
        // ShowDialog() calls inside them) to ever run. Application.Run() pumps until
        // Application.Exit() is called - see ClientSearchForm.cs's FormClosed handler for where
        // that happens.
        Application.Run();
    }
}
