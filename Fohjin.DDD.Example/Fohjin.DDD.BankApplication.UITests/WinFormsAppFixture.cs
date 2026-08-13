using System.Diagnostics;
using System.Net.Http.Json;
using FlaUI.Core;
using FlaUI.UIA3;
using Microsoft.Playwright;

namespace Fohjin.DDD.BankApplication.UITests;

// Drives a real, end-to-end run of the retargeted WinForms app (docs/09-client-uis.md):
// starts the real Fohjin.DDD.Sts and Fohjin.DDD.WebApi processes (native `dotnet run`,
// not TestServer/WebApplicationFactory - the whole point is exercising the real HTTP+OIDC path
// the compiled Fohjin.DDD.BankApplication.exe actually uses), then launches the compiled exe
// itself with FOHJIN_TEST_BROWSER_EXECUTABLE pointed at a real Edge instance so this fixture can
// attach to the login page via Playwright's CDP client (see DesktopAuthService.cs's comment on
// why raw UI-Automation-over-arbitrary-browser-window is avoided).
public sealed class WinFormsAppFixture : IAsyncDisposable
{
    private const int StsPort = 5310;
    private const int WebApiPort = 5320;
    private const int RemoteDebuggingPort = 9333;

    private Process? _stsProcess;
    private Process? _webApiProcess;
    private Process? _appProcess;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public UIA3Automation Automation { get; } = new();
    public Application? App { get; private set; }

    public static string RepoRoot { get; } = FindRepoRoot();

    public async Task StartAsync()
    {
        _stsProcess = StartDotnetRun("Fohjin.DDD.Sts", StsPort, sts: true);
        await WaitForHttpReadyAsync($"http://127.0.0.1:{StsPort}/.well-known/openid-configuration");

        _webApiProcess = StartDotnetRun("Fohjin.DDD.WebApi", WebApiPort, sts: false);
        await WaitForHttpReadyAsync($"http://127.0.0.1:{WebApiPort}/api/clients", acceptUnauthorized: true);

        var edgePath = FindEdgeExecutable();
        var appExe = Path.Combine(RepoRoot, "Fohjin.DDD.BankApplication", "bin", "Debug", "net10.0-windows", "Fohjin.DDD.BankApplication.exe");
        if (!File.Exists(appExe))
            throw new FileNotFoundException($"Build Fohjin.DDD.BankApplication first - exe not found at {appExe}");

        var startInfo = new ProcessStartInfo(appExe)
        {
            WorkingDirectory = Path.GetDirectoryName(appExe),
            UseShellExecute = false,
        };
        startInfo.Environment["FOHJIN_TEST_BROWSER_EXECUTABLE"] = edgePath;
        startInfo.Environment["FOHJIN_TEST_REMOTE_DEBUGGING_PORT"] = RemoteDebuggingPort.ToString();
        _appProcess = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start Fohjin.DDD.BankApplication.exe");

        App = Application.Attach(_appProcess.Id);
    }

    public async Task<IPage> AttachToLoginBrowserAsync()
    {
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        // The app only starts Edge once it reaches DesktopAuthService.LoginAsync() - poll for
        // the CDP endpoint rather than assuming a fixed delay is enough.
        for (var attempt = 0; attempt < 60; attempt++)
        {
            try
            {
                _browser = await _playwright.Chromium.ConnectOverCDPAsync($"http://127.0.0.1:{RemoteDebuggingPort}");
                break;
            }
            catch
            {
                await Task.Delay(500);
            }
        }

        if (_browser is null)
            throw new TimeoutException("Could not attach to the test browser via CDP - did the app open it?");

        // Don't assume Contexts[0]/Pages[0] is the STS tab - Edge may open more than one
        // context/page (e.g. a blank first tab alongside the one navigated to the authorize
        // URL), and picking the wrong one manifests as a confusing "target closed" error once
        // that unrelated tab closes on its own, not as an obviously-wrong-page error.
        var seenUrls = new List<string>();
        for (var attempt = 0; attempt < 40; attempt++)
        {
            seenUrls.Clear();
            foreach (var context in _browser.Contexts)
            {
                foreach (var page in context.Pages)
                {
                    seenUrls.Add(page.Url);
                    if (page.Url.Contains($"127.0.0.1:{StsPort}", StringComparison.Ordinal))
                        return page;
                }
            }

            await Task.Delay(500);
        }

        throw new TimeoutException(
            $"No page navigated to the STS (127.0.0.1:{StsPort}) within the timeout. Pages seen: [{string.Join(", ", seenUrls)}]");
    }

    private static Process StartDotnetRun(string projectName, int port, bool sts)
    {
        var projectPath = Path.Combine(RepoRoot, projectName, $"{projectName}.csproj");
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
        _ = projectPath;
        _ = sts;

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

    private static string FindEdgeExecutable()
    {
        string[] candidates =
        [
            @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
            @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
        ];

        return candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException("Could not find msedge.exe for CDP-driven login automation.");
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
        Automation.Dispose();

        // Browser.CloseAsync() only disconnects the CDP session when the browser was attached
        // via ConnectOverCDPAsync rather than launched by Playwright itself - it does not
        // terminate the OS process. Chromium's browser process also deliberately detaches from
        // its launching parent's job object, so killing the WinForms app's process tree
        // (below) doesn't reliably take the Edge instance down with it either. Both left
        // msedge.exe processes running indefinitely across test runs until this was found -
        // find and kill only the specific instance this run launched (matched by its
        // --user-data-dir, not just "any msedge.exe"), so a real Edge session the person
        // running these tests has open is never touched.
        if (_browser is not null)
            await _browser.CloseAsync();
        _playwright?.Dispose();
        KillTestBrowserProcesses();

        KillProcessTree(_appProcess);
        KillProcessTree(_webApiProcess);
        KillProcessTree(_stsProcess);
    }

    private static void KillTestBrowserProcesses()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'msedge.exe'");
            foreach (var result in searcher.Get())
            {
                var commandLine = result["CommandLine"] as string;
                if (commandLine is null || !commandLine.Contains("fohjin-test-browser-profile", StringComparison.OrdinalIgnoreCase))
                    continue;

                var processId = (uint)result["ProcessId"];
                try
                {
                    Process.GetProcessById((int)processId).Kill(entireProcessTree: true);
                }
                catch
                {
                    // Already gone - fine.
                }
            }
        }
        catch
        {
            // Best-effort cleanup - WMI not being available shouldn't fail the test run.
        }
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
