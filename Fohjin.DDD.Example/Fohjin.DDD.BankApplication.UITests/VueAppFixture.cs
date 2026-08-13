using System.Diagnostics;
using Microsoft.Playwright;

namespace Fohjin.DDD.BankApplication.UITests;

// Vue equivalent of WinFormsAppFixture.cs/WpfAppFixture.cs - drives the real Vue dev server
// (Fohjin.DDD.WebUI, standalone `npm run dev`, not through Fohjin.DDD.AppHost) end to end
// against real, separately-running Fohjin.DDD.Sts and Fohjin.DDD.WebApi processes. Unlike the
// desktop fixtures, there's no CDP-attach dance here (CLAUDE.md) - the browser IS the app under
// test, so this launches and drives its own headless Chromium page directly.
public sealed class VueAppFixture : IAsyncDisposable
{
    private const int StsPort = 5310;
    private const int WebApiPort = 5320;
    private const int VuePort = 5173;

    private Process? _stsProcess;
    private Process? _webApiProcess;
    private Process? _vueProcess;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public IPage Page { get; private set; } = null!;

    public static string RepoRoot { get; } = FindRepoRoot();

    public async Task StartAsync()
    {
        _stsProcess = StartDotnetRun("Fohjin.DDD.Sts", StsPort);
        await WaitForHttpReadyAsync($"http://127.0.0.1:{StsPort}/.well-known/openid-configuration");

        _webApiProcess = StartDotnetRun("Fohjin.DDD.WebApi", WebApiPort);
        await WaitForHttpReadyAsync($"http://127.0.0.1:{WebApiPort}/api/clients", acceptUnauthorized: true);

        _vueProcess = StartNpmRunDev();
        await WaitForHttpReadyAsync($"http://localhost:{VuePort}/");

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        Page = await _browser.NewPageAsync(new BrowserNewPageOptions { ViewportSize = new ViewportSize { Width = 1280, Height = 800 } });
    }

    private static Process StartNpmRunDev()
    {
        var startInfo = new ProcessStartInfo("cmd.exe")
        {
            WorkingDirectory = Path.Combine(RepoRoot, "Fohjin.DDD.WebUI"),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add("npm run dev");

        // Node.js on this machine (CLAUDE.md): a broken node.bat/npm.bat shim sits earlier on
        // PATH than the real C:\Program Files\nodejs install and fails outside an interactive
        // terminal - exactly the situation a child process spawned here is in. Prepending the
        // real install here matches the same fix Fohjin.DDD.AppHost needs for this same reason.
        startInfo.Environment["PATH"] = $"""C:\Program Files\nodejs;{startInfo.Environment["PATH"]}""";

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start `npm run dev`");
        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, _) => { };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private static Process StartDotnetRun(string projectName, int port)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.Combine(RepoRoot, projectName),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--urls");
        startInfo.ArgumentList.Add($"http://127.0.0.1:{port}");
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start {projectName}");
        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, _) => { };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private static async Task WaitForHttpReadyAsync(string url, bool acceptUnauthorized = false)
    {
        using var httpClient = new HttpClient();
        for (var attempt = 0; attempt < 60; attempt++)
        {
            try
            {
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode || (acceptUnauthorized && (int)response.StatusCode == 401))
                    return;
            }
            catch
            {
                // Not up yet - keep polling.
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"{url} did not become ready in time");
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "Fohjin.DDD.sln")))
            dir = Path.GetDirectoryName(dir);

        return dir ?? throw new DirectoryNotFoundException("Could not locate Fohjin.DDD.sln above " + AppContext.BaseDirectory);
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
            await _browser.CloseAsync();
        _playwright?.Dispose();

        KillProcessTree(_vueProcess);
        KillProcessTree(_webApiProcess);
        KillProcessTree(_stsProcess);
    }

    private static void KillProcessTree(Process? process)
    {
        if (process is null || process.HasExited)
            return;

        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }
}
