using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fohjin.DDD.BankApplication.Auth;

// Desktop OIDC login (docs/11-migration-plan.md Phase 7's "system browser + loopback redirect,
// not an embedded WebView2" decision): opens the STS's real login page in the user's actual
// browser (Process.Start with UseShellExecute) and catches the redirect on a local HttpListener,
// same authorization-code + PKCE flow the Vue app drives via oidc-client-ts (Fohjin.DDD.WebUI/
// src/auth/authService.ts) and the STS itself has no idea it's talking to a desktop app rather
// than a browser SPA - same seeded dev-client, same /connect/authorize and /connect/token
// endpoints. No refresh token is requested (Fohjin.DDD.Sts/Program.cs's dev-client only grants
// GrantTypes.AuthorizationCode) - the access token (OpenIddict's default 1-hour lifetime) is
// held in memory for the process's lifetime; a session outlasting that would need to sign in
// again, which is an acceptable limitation for this dev sample rather than something worth
// building silent-renewal machinery for.
public class DesktopAuthService(HttpClient httpClient, IConfiguration configuration, ILogger<DesktopAuthService> logger)
{
    public string? AccessToken { get; private set; }

    public async Task LoginAsync(CancellationToken cancellationToken = default)
    {
        var authority = (configuration["Sts:Authority"] ?? throw new InvalidOperationException("Sts:Authority is not configured.")).TrimEnd('/');
        var clientId = configuration["Sts:ClientId"] ?? throw new InvalidOperationException("Sts:ClientId is not configured.");
        var redirectUri = configuration["Sts:LoopbackRedirectUri"] ?? throw new InvalidOperationException("Sts:LoopbackRedirectUri is not configured.");

        var codeVerifier = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var codeChallenge = Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
        var state = Base64UrlEncode(RandomNumberGenerator.GetBytes(16));

        using var listener = new HttpListener();
        listener.Prefixes.Add(redirectUri);
        listener.Start();

        var authorizeUrl =
            $"{authority}/connect/authorize?response_type=code&client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={Uri.EscapeDataString("openid profile email")}" +
            $"&code_challenge={codeChallenge}&code_challenge_method=S256&state={state}";

        logger.LogInformation("Opening system browser for sign-in at {Authority}", authority);
        Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true });

        HttpListenerContext context;
        try
        {
            context = await listener.GetContextAsync().WaitAsync(cancellationToken);
        }
        finally
        {
            listener.Stop();
        }

        var query = context.Request.QueryString;
        var receivedState = query["state"];
        var code = query["code"];
        var error = query["error"];

        await RespondWithClosePageAsync(context, cancellationToken);

        if (error is not null)
            throw new InvalidOperationException($"Sign-in failed: {error}");
        if (receivedState != state)
            throw new InvalidOperationException("Sign-in failed: state mismatch (possible CSRF).");
        if (string.IsNullOrEmpty(code))
            throw new InvalidOperationException("Sign-in failed: no authorization code was returned.");

        var tokenResponse = await httpClient.PostAsync($"{authority}/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = clientId,
            ["code_verifier"] = codeVerifier,
        }), cancellationToken);
        tokenResponse.EnsureSuccessStatusCode();

        var payload = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);
        AccessToken = payload?.AccessToken ?? throw new InvalidOperationException("Token response did not include an access_token.");
        logger.LogInformation("Signed in successfully");
    }

    private static async Task RespondWithClosePageAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var buffer = Encoding.UTF8.GetBytes("<html><body>Signed in - you can close this window and return to the app.</body></html>");
        context.Response.ContentType = "text/html";
        context.Response.ContentLength64 = buffer.Length;
        await context.Response.OutputStream.WriteAsync(buffer, cancellationToken);
        context.Response.OutputStream.Close();
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private record TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }
    }
}
