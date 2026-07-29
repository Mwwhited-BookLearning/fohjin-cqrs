using System.Net.Http.Headers;

namespace Fohjin.DDD.DesktopClient;

// Attaches the token DesktopAuthService obtained via the system-browser + loopback PKCE flow
// to every call FohjinApiClient makes - the generated client has no auth concept of its own,
// it just uses whatever HttpClient it's constructed with (same pattern as Fohjin.DDD.WebUI's
// src/api/client.ts authenticatedFetch wrapper).
public class AuthorizationHandler(DesktopAuthService authService) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (authService.AccessToken is { } token)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return base.SendAsync(request, cancellationToken);
    }
}
