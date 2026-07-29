using System.Diagnostics;
using FlaUI.Core;
using FlaUI.UIA3;
using Microsoft.Playwright;

namespace Fohjin.DDD.BankApplication.UITests;

// WPF equivalent of WinFormsAppFixture.cs - drives the real compiled
// Fohjin.DDD.BankApplication.Wpf.exe end to end against real, separately-running
// Fohjin.DDD.Sts and Fohjin.DDD.WebApi processes. Uses its own loopback port (5340, not
// WinForms' 5330) and its own CDP remote-debugging port so both fixtures could in principle
// run side by side without colliding.
public sealed class WpfAppFixture : IAsyncDisposable
{
    private const int StsPort = 5310;
    private const int WebApiPort = 5320;
    private const int RemoteDebuggingPort = 9334;

    private Process? _stsProcess;
    private Process? _webApiProcess;
    private Process? _appProcess;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public UIA3Automation Automation { get; } = new();
    public int ProcessId { get; private set; }

    public static string RepoRoot { get; } = FindRepoRoot();

    public async Task StartAsync()
    {
        _stsProcess = StartDotnetRun("Fohjin.DDD.Sts", StsPort);
        await WaitForHttpReadyAsync($"http://127.0.0.1:{StsPort}/.well-known/openid-configuration");

        _webApiProcess = StartDotnetRun("Fohjin.DDD.WebApi", WebApiPort);
        await WaitForHttpReadyAsync($"http://127.0.0.1:{WebApiPort}/api/clients", acceptUnauthorized: true);

        var edgePath = FindEdgeExecutable();
        var appExe = Path.Combine(RepoRoot, "Fohjin.DDD.BankApplication.Wpf", "bin", "Debug", "net10.0-windows", "Fohjin.DDD.BankApplication.Wpf.exe");
        if (!File.Exists(appExe))
            throw new FileNotFoundException($"Build Fohjin.DDD.BankApplication.Wpf first - exe not found at {appExe}");

        var startInfo = new ProcessStartInfo(appExe)
        {
            WorkingDirectory = Path.GetDirectoryName(appExe),
            UseShellExecute = false,
        };
        startInfo.Environment["FOHJIN_TEST_BROWSER_EXECUTABLE"] = edgePath;
        startInfo.Environment["FOHJIN_TEST_REMOTE_DEBUGGING_PORT"] = RemoteDebuggingPort.ToString();
        _appProcess = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start Fohjin.DDD.BankApplication.Wpf.exe");

        ProcessId = _appProcess.Id;
    }

    public async Task<IPage> AttachToLoginBrowserAsync()
    {
        _playwright = await Playwright.CreateAsync();

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

        // See WinFormsAppFixture.cs's DisposeAsync comment - CloseAsync() only disconnects the
        // CDP session for a browser attached via ConnectOverCDPAsync, it doesn't terminate the
        // OS process, and Chromium detaches from its launching parent's job object - both leave
        // msedge.exe running indefinitely otherwise. Match only the instance this run launched.
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
