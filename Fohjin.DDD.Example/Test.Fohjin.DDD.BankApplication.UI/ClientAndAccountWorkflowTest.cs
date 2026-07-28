using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Fohjin.DDD.BankApplication.UI;

// End-to-end UI automation for the retargeted WinForms app (docs/11-migration-plan.md Phase 7) -
// the desktop equivalent of the Playwright scripts used to verify Fohjin.DDD.WebUI in Phase 6.
// Drives the real compiled Fohjin.DDD.BankApplication.exe against real, separately-running
// Fohjin.DDD.Sts and Fohjin.DDD.WebApi processes: sign in through the real STS login page,
// create a client, open it, open a new account, deposit cash, and confirm the balance updates -
// proving the whole HTTP-backed presenter chain (not just each presenter's unit tests in
// isolation) actually works end to end.
[TestClass]
[TestCategory("ui")]
public class ClientAndAccountWorkflowTest
{
    private static WinFormsAppFixture _fixture = null!;

    [ClassInitialize]
    public static async Task ClassSetup(TestContext _)
    {
        _fixture = new WinFormsAppFixture();
        await _fixture.StartAsync();
    }

    [ClassCleanup]
    public static async Task ClassTeardown()
    {
        await _fixture.DisposeAsync();
    }

    [TestMethod]
    public async Task Signing_in_creating_a_client_and_depositing_cash_works_end_to_end()
    {
        var clientName = $"UI Test Client {Guid.NewGuid():N}";

        await SignInAsync();

        // The redirect chain back through the loopback listener plus the token exchange takes
        // a moment after clicking the login form's submit button - the search window (shown
        // from Program.cs right after DesktopAuthService.LoginAsync() completes) won't exist
        // the instant SignInAsync() returns.
        var searchWindow = WaitForWindow("Client Search Form", timeoutSeconds: 30);
        ClickMenuItem(searchWindow, "Client", "Add a new client");

        var wizardWindow = WaitForWindow("Client Details");
        FillTextBox(wizardWindow, "_clientName", clientName);
        ClickButton(wizardWindow, "_clientNameSaveButton");

        FillTextBox(wizardWindow, "_street", "Test Street");
        FillTextBox(wizardWindow, "_streetNumber", "1");
        FillTextBox(wizardWindow, "_postalCode", "1000");
        FillTextBox(wizardWindow, "_city", "Oslo");
        ClickButton(wizardWindow, "_addressSaveButton");

        FillTextBox(wizardWindow, "_phoneNumber", "12345678");
        ClickButton(wizardWindow, "_phoneNumberSaveButton");

        // The create-wizard closes its own window once CreateClientAsync completes. Querying a
        // window mid-teardown (exactly what's happening here) can throw the same transient
        // COMException TryGetTitle already guards against.
        WaitUntil(() => IsWindowGoneOrOffscreen(wizardWindow) || GetWindowByTitlePart("Client Details") is null, TimeSpan.FromSeconds(10));

        var clientItem = WaitForListItemContaining(searchWindow, "_clients", clientName, TimeSpan.FromSeconds(15));
        Assert.IsNotNull(clientItem, $"'{clientName}' did not appear in the client search list");

        // ClientSearchForm.cs wires OnOpenSelectedClient to the ListBox's own Click event, not
        // a per-item DoubleClick - a single click both selects the item and fires it, matching
        // real usage (unlike ClientDetails.cs's _accounts list, which really is wired to
        // DoubleClick - see below). Dev databases persist across every run of this suite (and
        // every manual debugging session), so by now the list has many more entries than fit
        // in the visible viewport - scroll the item into view first, or it has no on-screen
        // point to click at all.
        ScrollIntoView(clientItem!);
        clientItem!.Click();
        var detailsWindow = WaitForWindow("Client Details");

        // Clicking the menu item switches ClientDetails.cs's tabControl1 to the "add new
        // account" tab (ClientDetailsPresenter.InitiateOpenNewAccount -> EnableAddNewAccountPanel)
        // - _newAccountName lives on that tab, so it isn't fillable (or found by UIA at all)
        // until this happens first, same lesson as the top-level menu needing to be opened
        // before its child item is visible.
        ClickMenuItem(detailsWindow, "Accounts", "Add new account");
        FillTextBox(detailsWindow, "_newAccountName", "Main Account");
        ClickButton(detailsWindow, "_newAccountCreateButton");

        var accountItem = WaitForListItemContaining(detailsWindow, "_accounts", "Main Account", TimeSpan.FromSeconds(15));
        Assert.IsNotNull(accountItem, "the opened account did not appear in the client's account list");

        ScrollIntoView(accountItem!);
        accountItem!.DoubleClick();
        var accountWindow = WaitForWindow("Account Details");

        ClickMenuItem(accountWindow, "Transfer", "Make cash deposit");
        FillTextBox(accountWindow, "_depositAmount", "100");
        ClickButton(accountWindow, "_depositButton");

        var balanceLabel = WaitForLabelValue(accountWindow, "_balanceLabel", "100", TimeSpan.FromSeconds(15));
        Assert.AreEqual("100", balanceLabel, "balance did not update to 100 after depositing cash");
    }

    private async Task SignInAsync()
    {
        var page = await _fixture.AttachToLoginBrowserAsync();
        Console.WriteLine($"[diag] login page url: {page.Url}");
        await page.WaitForSelectorAsync("#Input_Email", new PageWaitForSelectorOptions { Timeout = 30000 });
        await page.FillAsync("#Input_Email", "dev@fohjin.local");
        await page.FillAsync("#Input_Password", "Dev!Passw0rd");
        await page.ClickAsync("#login-submit");
        await Task.Delay(2000);
        Console.WriteLine($"[diag] after submit, url: {page.Url}");
    }

    // Querying .Title on a window that's mid-construction/mid-teardown can throw a transient
    // COMException ("An event was unable to invoke any of the subscribers") straight from the
    // native UI Automation client - not a real failure, just a race between this polling loop
    // and the window's own lifecycle. Swallow it and retry rather than letting one bad instant
    // fail the whole wait.
    private static string? TryGetTitle(Window window)
    {
        try
        {
            return window.Title;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return null;
        }
    }

    // Treats the same transient COMException as "yes, it's gone" - querying a window mid-
    // teardown is exactly when a real answer would be true anyway.
    private static bool IsWindowGoneOrOffscreen(Window window)
    {
        try
        {
            return window.IsOffscreen;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return true;
        }
    }

    // FlaUI's Application.GetAllTopLevelWindows(automation) reliably misses at least one real,
    // visible, correctly-titled top-level window belonging to this process (confirmed: raw
    // Win32 EnumWindows sees "Client Search Form" the whole time GetAllTopLevelWindows reports
    // only "Monitoring") - a FlaUI/UIA limitation with this app's windows, not a real absence.
    // Finding the HWND via plain Win32 first and wrapping only THAT specific handle through
    // FlaUI (AutomationBase.FromHandle) sidesteps whatever GetAllTopLevelWindows' scope query
    // is failing to enumerate.
    private static Window? GetWindowByTitlePart(string titlePart)
    {
        var hwnd = IntPtr.Zero;
        NativeMethods.EnumWindows((candidate, _) =>
        {
            NativeMethods.GetWindowThreadProcessId(candidate, out var windowProcessId);
            if (windowProcessId != _fixture.App!.ProcessId || !NativeMethods.IsWindowVisible(candidate))
                return true;

            var length = NativeMethods.GetWindowTextLength(candidate);
            if (length == 0)
                return true;

            var builder = new System.Text.StringBuilder(length + 1);
            NativeMethods.GetWindowText(candidate, builder, builder.Capacity);
            if (!builder.ToString().Contains(titlePart, StringComparison.OrdinalIgnoreCase))
                return true;

            hwnd = candidate;
            return false;
        }, IntPtr.Zero);

        if (hwnd == IntPtr.Zero)
            return null;

        return _fixture.Automation.FromHandle(hwnd).AsWindow();
    }

    // WinForms ToolStripMenuItems don't support UIA's AutomationId property at all (throws
    // FlaUI.Core.Exceptions.PropertyNotSupportedException - confirmed by dumping the full
    // descendant tree; ToolStrip items are owner-drawn by the strip, not real HWND-backed
    // controls the way TextBox/Button/ListBox are, so their accessibility implementation is
    // more limited) - menu items have to be found by their Name (display text) instead. They
    // also only get realized as descendants once the parent dropdown is actually open -
    // FindFirstDescendant for a leaf item returns null until the top-level menu has been
    // clicked, exactly like a real user would have to click it first to see the item at all.
    private static void ClickMenuItem(Window window, string topMenuName, string itemName)
    {
        var topMenu = window.FindFirstDescendant(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName(topMenuName)));
        Assert.IsNotNull(topMenu, $"could not find top-level menu '{topMenuName}'");
        topMenu!.Click();

        FlaUI.Core.AutomationElements.AutomationElement? item = null;
        for (var attempt = 0; attempt < 20 && item is null; attempt++)
        {
            item = window.FindFirstDescendant(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName(itemName)));
            if (item is null)
                Thread.Sleep(200);
        }

        Assert.IsNotNull(item, $"could not find menu item '{itemName}' under '{topMenuName}'");
        Invoke(item!);
    }

    private static Window WaitForWindow(string titlePart, int timeoutSeconds = 15)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            var window = GetWindowByTitlePart(titlePart);
            if (window is not null)
                return window;

            var titles = _fixture.App!.GetAllTopLevelWindows(_fixture.Automation).Select(w => TryGetTitle(w) ?? "<unavailable>");
            var rawTitles = Win32EnumProcessWindowTitles(_fixture.App.ProcessId);
            Console.WriteLine($"[diag] waiting for '{titlePart}', FlaUI windows: [{string.Join(", ", titles)}], raw Win32 windows: [{string.Join(", ", rawTitles)}], app.HasExited={_fixture.App.HasExited}");
            Thread.Sleep(1000);
        }

        throw new TimeoutException($"No window with title containing '{titlePart}' appeared within {timeoutSeconds}s");
    }

    // Bypasses FlaUI/UI Automation entirely to check, at the raw Win32 level, whether the
    // process has any top-level window at all - narrows down whether a missing window is a
    // UIA-visibility problem or the window genuinely doesn't exist yet.
    private static List<string> Win32EnumProcessWindowTitles(int processId)
    {
        var titles = new List<string>();
        NativeMethods.EnumWindows((hWnd, _) =>
        {
            NativeMethods.GetWindowThreadProcessId(hWnd, out var windowProcessId);
            if (windowProcessId == processId && NativeMethods.IsWindowVisible(hWnd))
            {
                var length = NativeMethods.GetWindowTextLength(hWnd);
                if (length > 0)
                {
                    var builder = new System.Text.StringBuilder(length + 1);
                    NativeMethods.GetWindowText(hWnd, builder, builder.Capacity);
                    titles.Add(builder.ToString());
                }
                else
                {
                    titles.Add("<empty title>");
                }
            }

            return true;
        }, IntPtr.Zero);

        return titles;
    }

    private static class NativeMethods
    {
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
    }

    // Deliberately always a real (async) mouse click, never UIA's InvokePattern.Invoke() - that
    // call is a synchronous, blocking round-trip into the target's message processing, and
    // every menu item this app has opens a *modal* dialog from its click handler
    // (ClientDetailsPresenter/AccountDetailsPresenter's Display() calls ShowDialog()). Invoke()
    // doesn't return until that whole nested modal loop finishes closing - which can't happen
    // until this test interacts with the dialog Invoke() is still blocked waiting to return
    // from - a guaranteed deadlock, confirmed by an actual UIA "Operation timed out" COMException.
    private static void Invoke(FlaUI.Core.AutomationElements.AutomationElement element) => element.Click();

    // Dev databases persist across every run of this suite (and every manual debugging
    // session against the same STS/WebApi), so a list can easily have more entries than fit
    // the visible viewport by now - clicking an off-screen item throws NoClickablePointException,
    // confirmed live. ScrollItemPattern is the correct UIA way to bring it into view without
    // relying on a screen coordinate that doesn't exist yet.
    private static void ScrollIntoView(FlaUI.Core.AutomationElements.AutomationElement element)
    {
        if (element.Patterns.ScrollItem.IsSupported)
            element.Patterns.ScrollItem.Pattern.ScrollIntoView();
    }

    private static void FillTextBox(Window window, string automationId, string value)
    {
        var textBox = window.FindFirstDescendant(cf => cf.ByAutomationId(automationId))?.AsTextBox();
        Assert.IsNotNull(textBox, $"textbox '{automationId}' not found in window '{window.Title}'");
        textBox!.Text = value;
    }

    private static void ClickButton(Window window, string automationId)
    {
        var button = window.FindFirstDescendant(cf => cf.ByAutomationId(automationId))?.AsButton();
        Assert.IsNotNull(button, $"button '{automationId}' not found in window '{window.Title}'");
        // Same reasoning as Invoke() above - a real click, not Button.Invoke()'s InvokePattern
        // RPC call, since the last wizard step's save button closes its own dialog from the
        // click handler.
        button!.Click();
    }

    private static ListBoxItem? WaitForListItemContaining(Window window, string listAutomationId, string text, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            var listBox = window.FindFirstDescendant(cf => cf.ByAutomationId(listAutomationId))?.AsListBox();
            var match = listBox?.Items.FirstOrDefault(i => i.Text.Contains(text, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match;

            Thread.Sleep(500);
        }

        return null;
    }

    private static string? WaitForLabelValue(Window window, string automationId, string expectedContains, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        string? lastValue = null;
        while (DateTime.UtcNow < deadline)
        {
            var label = window.FindFirstDescendant(cf => cf.ByAutomationId(automationId))?.AsLabel();
            lastValue = label?.Text;
            if (lastValue is not null && lastValue.Contains(expectedContains, StringComparison.OrdinalIgnoreCase))
                return lastValue;

            Thread.Sleep(500);
        }

        return lastValue;
    }

    private static void WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;

            Thread.Sleep(300);
        }
    }
}
