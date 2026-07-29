namespace Fohjin.DDD.DesktopClient;

// Bound from each desktop client's own "WebApi" appsettings.json section - shared shape since
// WinForms and WPF both just need a single base address to point FohjinApiClient/EventStreamClient
// at (see Fohjin.DDD.BankApplication/Program.cs and Fohjin.DDD.BankApplication.Wpf/App.xaml.cs).
public sealed class WebApiOptions
{
    public const string SectionName = "WebApi";

    public string? BaseUrl { get; set; }
}
