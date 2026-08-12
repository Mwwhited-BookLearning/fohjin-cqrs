namespace Fohjin.DDD.Sts;

// Bound from the "DevClient" appsettings.json section - the one seeded OAuth2/OIDC client every
// caller (Vue, WinForms, WPF, Scalar) authenticates as (docs/00-architecture-overview.md). Kept
// configurable rather than hard-coded so adding a new caller is an appsettings change, not a
// code change - see Program.cs's dev-client seeding.
public sealed class DevClientOptions
{
    public const string SectionName = "DevClient";

    public string? ClientId { get; set; }
    public string? DisplayName { get; set; }
    public string[] RedirectUris { get; set; } = [];
    public string[] PostLogoutRedirectUris { get; set; } = [];
}
