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

        services.AddHttpClient<DesktopAuthService>();
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
    }
}
