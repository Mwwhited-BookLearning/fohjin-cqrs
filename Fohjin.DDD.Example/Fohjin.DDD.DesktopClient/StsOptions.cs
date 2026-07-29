namespace Fohjin.DDD.DesktopClient;

// Bound from each desktop client's own "Sts" appsettings.json section via
// services.AddOptions<StsOptions>().Bind(...).ValidateOnStart() - see DesktopAuthService.cs.
public sealed class StsOptions
{
    public const string SectionName = "Sts";

    public string? Authority { get; set; }
    public string? ClientId { get; set; }
    public string? LoopbackRedirectUri { get; set; }
}
