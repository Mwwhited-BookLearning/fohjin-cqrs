using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Test.Fohjin.DDD.ApiClient;

// Phase 5 added .RequireAuthorization() to the command/query/SSE endpoints, backed by real
// JwtBearer validation against the dev STS's discovery document. Tests that exercise those
// endpoints' own behavior (OData filtering, the generated NSwag client, ...) aren't testing
// authentication itself, so WebApiIntegrationTestFixture swaps in this always-succeeds scheme
// as the default - standard ASP.NET Core practice for testing [Authorize]-protected endpoints
// without standing up a real token issuer. AuthenticationRequiredTest verifies the real
// JwtBearer wiring (unauthenticated -> 401) with this handler NOT in play.
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "test-user")], SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
