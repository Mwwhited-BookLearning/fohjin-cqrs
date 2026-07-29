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
        LaunchBrowser(authorizeUrl);

        // listener.Stop() must NOT run until after the response below is written - it tears
        // down resources (e.g. the context's response stream's ThreadPoolBoundHandle) shared
        // with any still-in-flight HttpListenerContext, so calling it right after
        // GetContextAsync() (before RespondWithClosePageAsync's WriteAsync) throws
        // ObjectDisposedException. The `using var listener` above already stops/disposes it
        // once this method returns or throws, so nothing extra is needed here.
        var context = await listener.GetContextAsync().WaitAsync(cancellationToken);

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

    // Test seam only - production always takes the UseShellExecute path (the user's actual
    // default browser). FOHJIN_TEST_BROWSER_EXECUTABLE lets the FlaUI UI automation suite
    // (Test.Fohjin.DDD.BankApplication.UI) point this at a Chromium/Edge binary launched with
    // remote debugging enabled, so the test can attach via Playwright's CDP client and drive
    // the real STS login form the same way a human would, instead of trying to automate an
    // arbitrary OS-default browser window through raw UI Automation.
    private static void LaunchBrowser(string authorizeUrl)
    {
        var testBrowserExecutable = Environment.GetEnvironmentVariable("FOHJIN_TEST_BROWSER_EXECUTABLE");
        if (string.IsNullOrEmpty(testBrowserExecutable))
        {
            Process.Start(new ProcessStartInfo(authorizeUrl) { UseShellExecute = true });
            return;
        }

        var remoteDebuggingPort = Environment.GetEnvironmentVariable("FOHJIN_TEST_REMOTE_DEBUGGING_PORT") ?? "9333";
        // A fresh, uniquely-named profile directory every launch, not a fixed reused path -
        // reusing the same directory let Edge restore the previous run's leftover tabs
        // (including a stale STS /connect/authorize redirect) instead of opening only the new
        // authorizeUrl, which made the test attach to the wrong page.
        var profileDirectory = Path.Combine(Path.GetTempPath(), $"fohjin-test-browser-profile-{Guid.NewGuid():N}");
        Process.Start(new ProcessStartInfo(testBrowserExecutable)
        {
            ArgumentList =
            {
                $"--remote-debugging-port={remoteDebuggingPort}",
                "--no-first-run",
                "--no-default-browser-check",
                "--disable-session-crashed-bubble",
                $"--user-data-dir={profileDirectory}",
                authorizeUrl,
            },
            UseShellExecute = false,
        });
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
