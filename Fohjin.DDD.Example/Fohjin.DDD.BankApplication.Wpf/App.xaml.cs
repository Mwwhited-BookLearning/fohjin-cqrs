using System.Net.Http;
using System.Windows;
using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Presenters;
using Fohjin.DDD.BankApplication.Wpf.Services;
using Fohjin.DDD.BankApplication.Wpf.ViewModels;
using Fohjin.DDD.BankApplication.Wpf.Views;
using Fohjin.DDD.Common;
using Fohjin.DDD.DesktopClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fohjin.DDD.BankApplication.Wpf;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(e.Args)
            .Build();

        var monitoringLoggerProvider = new MonitoringLoggerProvider();

        // Same shape as Fohjin.DDD.BankApplication/Program.cs (WinForms): every operation is an
        // HTTP call to Fohjin.DDD.WebApi through the generated FohjinApiClient, AuthorizationHandler
        // attaches the token DesktopAuthService obtained, HttpCallLoggingHandler feeds the
        // monitoring window's log half.
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
            .AddCommonServices();

        services.AddTransient<AuthorizationHandler>();
        services.AddTransient<HttpCallLoggingHandler>();

        // Singleton, not a typed client (same reasoning as WinForms' Program.cs):
        // AuthorizationHandler needs to read back the SAME instance's AccessToken that OnStartup
        // sets via LoginAsync() below.
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

        // No HttpCallLoggingHandler here - it logs once per SendAsync, which for a long-lived SSE
        // connection would only ever log the single initial "GET api/events" call.
        services.AddHttpClient<EventStreamClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["WebApi:BaseUrl"]
                ?? throw new InvalidOperationException("WebApi:BaseUrl is not configured."));
        })
            .AddHttpMessageHandler<AuthorizationHandler>();

        services.AddSingleton<DomainEventBus>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();

        services.AddTransient<ClientSearchViewModel>();
        services.AddTransient<ClientCreateViewModel>();
        services.AddTransient<ClientDetailsViewModel>();
        services.AddTransient<AccountDetailsViewModel>();
        services.AddSingleton<MonitoringViewModel>();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MonitoringWindow>();

        _serviceProvider = services.BuildServiceProvider();

        // Desktop OIDC login happens once, up front, before any window is shown - same decision
        // as WinForms' Program.cs, since every ViewModel's FohjinApiClient calls assume a
        // signed-in AuthorizationHandler.AccessToken is already in place by the time they run.
        await _serviceProvider.GetRequiredService<DesktopAuthService>().LoginAsync();

        _serviceProvider.GetRequiredService<DomainEventBus>().Start();

        _serviceProvider.GetRequiredService<MonitoringWindow>().Show();

        var navigation = _serviceProvider.GetRequiredService<INavigationService>();
        navigation.ShowClientSearch();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
