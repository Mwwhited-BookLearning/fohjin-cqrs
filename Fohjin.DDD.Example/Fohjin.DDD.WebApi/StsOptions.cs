namespace Fohjin.DDD.WebApi;

// Bound from the "Sts" appsettings.json section - just the one value this API itself needs
// (validating tokens' issuer via OIDC discovery, and describing the same STS as an OAuth2
// security scheme in the generated OpenAPI document). The desktop clients bind a differently
// shaped StsOptions from the same section name (Fohjin.DDD.DesktopClient/StsOptions.cs) since
// they additionally need ClientId/LoopbackRedirectUri for the loopback login flow itself.
public sealed class StsOptions
{
    public const string SectionName = "Sts";

    public string Authority { get; set; } = "http://127.0.0.1:5310/";
    public bool RequireHttpsMetadata { get; set; } = true;
}
