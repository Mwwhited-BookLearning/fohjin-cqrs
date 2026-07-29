using FlaUI.Core.AutomationElements;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fohjin.DDD.BankApplication.UITests;

// WPF equivalent of ClientAndAccountWorkflowTest.cs - drives the real compiled
// Fohjin.DDD.BankApplication.Wpf.exe end to end: sign in, create a client, open it, open a
// new account, deposit cash, and exercise the full bank-card lifecycle (assign, cancel,
// report stolen). Added alongside the WPF client itself per the project convention that a
// new UI (or a feature added to an existing one) isn't done until it has UI automation
// coverage, not just a passing `dotnet build`.
//
// This exact flow surfaced three real bugs during manual live-verification before this test
// existed (docs/09-client-uis.md, CLAUDE.md's bug list): an SSE JSON-casing mismatch that
// silently broke every event-driven reload, a ListBox double-click MouseBinding that never
// fired, and (WinForms-side, found while cross-checking) a bank-card Assign button that could
// stay permanently disabled. This test exists specifically so a regression in any of those
// paths fails a build instead of requiring another live-verification pass to notice.
[TestClass]
[TestCategory("ui")]
public class WpfClientAndAccountWorkflowTest
{
    private static WpfAppFixture _fixture = null!;

    [ClassInitialize]
    public static async Task ClassSetup(TestContext _)
    {
        _fixture = new WpfAppFixture();
        await _fixture.StartAsync();
    }

    [ClassCleanup]
    public static async Task ClassTeardown()
    {
        await _fixture.DisposeAsync();
    }

    [TestMethod]
    public async Task Signing_in_creating_a_client_and_managing_accounts_and_bank_cards_works_end_to_end()
    {
        var clientName = $"UI Test WPF Client {Guid.NewGuid():N}";

        await SignInAsync();

        var mainWindow = Win32WindowFinder.WaitForWindow(_fixture.Automation, _fixture.ProcessId, "Fohjin Bank", timeoutSeconds: 30);
        Assert.IsNotNull(mainWindow, "MainWindow ('Fohjin Bank') did not appear");
        var monitoring = Win32WindowFinder.WaitForWindow(_fixture.Automation, _fixture.ProcessId, "Monitoring", timeoutSeconds: 15);
        Assert.IsNotNull(monitoring, "Monitoring window did not appear");

        // MainWindow and MonitoringWindow both default to Manual placement with no explicit
        // position, so they can overlap on screen - a click computed from MainWindow's own
        // bounding rect could land on the Monitoring window instead if it's topmost there.
        var mainHwnd = Win32WindowFinder.FindHwnd(_fixture.ProcessId, "Fohjin Bank");
        Win32WindowFinder.BringToForeground(mainHwnd);

        Assert.IsNotNull(WaitForElement(mainWindow!, "_clients", 15), "ClientSearchView did not load (_clients not found)");

        ClickById(mainWindow!, "_newClientButton");
        Assert.IsNotNull(WaitForElement(mainWindow!, "_clientName", 10), "ClientCreateView did not appear");
        SetText(mainWindow!, "_clientName", clientName);
        SetText(mainWindow!, "_street", "Test Street");
        SetText(mainWindow!, "_streetNumber", "1");
        SetText(mainWindow!, "_postalCode", "1000");
        SetText(mainWindow!, "_city", "Oslo");
        SetText(mainWindow!, "_phoneNumber", "12345678");
        ClickById(mainWindow!, "_createClientButton");

        DoubleClickListItemUntilNavigated(mainWindow!, mainHwnd, "_clients", clientName, "_accounts");

        Assert.IsNotNull(WaitForElement(mainWindow!, "_accounts", 15), "ClientDetailsView did not appear");
        var nameBox = WaitForElement(mainWindow!, "_clientName", 10)?.AsTextBox();
        Assert.AreEqual(clientName, nameBox?.Text, "ClientDetails name mismatch after navigating from search");

        SetText(mainWindow!, "_newAccountName", "Main Account");
        InvokeById(mainWindow!, "_openAccountButton");
        var accountItem = WaitForListItem(mainWindow!, "_accounts", "Main Account", 20);
        Assert.IsNotNull(accountItem, "the opened account did not appear in the client's account list");

        // Bank cards: assign a new card to the account just opened, then exercise its lifecycle.
        var accountCombo = WaitForElement(mainWindow!, "_newBankCardAccount", 10)?.AsComboBox();
        Assert.IsNotNull(accountCombo, "_newBankCardAccount combo not found");
        for (var attempt = 0; attempt < 20 && accountCombo!.Items.Length == 0; attempt++)
            Thread.Sleep(500);
        Assert.IsTrue(accountCombo!.Items.Length > 0, "_newBankCardAccount combo has no items");
        accountCombo.Select(0);
        InvokeById(mainWindow!, "_assignBankCardButton");

        var activeCard = WaitForListItemContaining(mainWindow!, "_bankCards", "Active", 20);
        Assert.IsNotNull(activeCard, "assigned bank card did not appear with Active status");

        if (activeCard!.Patterns.SelectionItem.IsSupported)
            activeCard.Patterns.SelectionItem.Pattern.Select();
        else
        {
            Win32WindowFinder.ScrollIntoView(activeCard);
            activeCard.Click();
        }
        InvokeById(mainWindow!, "_cancelBankCardButton");
        var cancelledCard = WaitForListItemContaining(mainWindow!, "_bankCards", "Cancelled", 20);
        Assert.IsNotNull(cancelledCard, "bank card did not transition to Cancelled after Cancel");

        // Open the account and deposit cash, mirroring ClientAndAccountWorkflowTest.cs's
        // WinForms coverage of the same underlying command/read-model path.
        DoubleClickListItemUntilNavigated(mainWindow!, mainHwnd, "_accounts", "Main Account", "_accountName");

        var accountNameBox = WaitForElement(mainWindow!, "_accountName", 15)?.AsTextBox();
        Assert.AreEqual("Main Account", accountNameBox?.Text, "AccountDetailsView did not load the expected account");

        SetText(mainWindow!, "_depositAmount", "100");
        InvokeById(mainWindow!, "_depositButton");

        var balanceText = WaitForElementTextContaining(mainWindow!, "_accountSummary", "100", 20);
        Assert.IsTrue(balanceText?.Contains("100", StringComparison.Ordinal) ?? false,
            $"balance did not update to reflect a 100 deposit - last seen: '{balanceText}'");
    }

    private async Task SignInAsync()
    {
        var page = await _fixture.AttachToLoginBrowserAsync();
        await page.WaitForSelectorAsync("#Input_Email", new PageWaitForSelectorOptions { Timeout = 30000 });
        await page.FillAsync("#Input_Email", "dev@fohjin.local");
        await page.FillAsync("#Input_Password", "Dev!Passw0rd");
        await page.ClickAsync("#login-submit");
        await Task.Delay(2000);
    }

    private static AutomationElement? WaitForElement(Window window, string automationId, int timeoutSeconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            var element = window.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
            if (element is not null)
                return element;

            Thread.Sleep(300);
        }

        return null;
    }

    private static string? WaitForElementTextContaining(Window window, string automationId, string expectedSubstring, int timeoutSeconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        string? lastText = null;
        while (DateTime.UtcNow < deadline)
        {
            lastText = window.FindFirstDescendant(cf => cf.ByAutomationId(automationId))?.Name;
            if (lastText is not null && lastText.Contains(expectedSubstring, StringComparison.Ordinal))
                return lastText;

            Thread.Sleep(500);
        }

        return lastText;
    }

    // Double-clicking a freshly-rendered ListBoxItem occasionally races the item's own layout
    // pass - GetClickablePoint() can throw NoClickablePointException for a bounding rect that's
    // momentarily stale (found live: happens more often right after a list just repopulated
    // from a live event-driven reload than on a settled list). Re-fetching the item fresh and
    // retrying is cheap insurance against that one-frame race; it does not mask the double-click
    // MouseBinding bug this test guards against, since that failure mode is "navigation never
    // happens even though the click lands cleanly," not an exception.
    private static void DoubleClickListItemUntilNavigated(Window window, IntPtr windowHwnd, string listAutomationId, string itemText, string navigatedToAutomationId)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var item = WaitForListItem(window, listAutomationId, itemText, attempt == 0 ? 45 : 5);
            Assert.IsNotNull(item, $"'{itemText}' did not appear in '{listAutomationId}'");

            Win32WindowFinder.ScrollIntoView(item!);
            Win32WindowFinder.BringToForeground(windowHwnd);
            Thread.Sleep(200);
            try
            {
                item!.DoubleClick();
            }
            catch (FlaUI.Core.Exceptions.NoClickablePointException)
            {
                continue;
            }

            if (WaitForElement(window, navigatedToAutomationId, 5) is not null)
                return;
        }

        Assert.Fail($"Double-clicking '{itemText}' in '{listAutomationId}' never navigated to a view with '{navigatedToAutomationId}'");
    }

    private static void ClickById(Window window, string automationId)
    {
        var element = WaitForElement(window, automationId, 10);
        Assert.IsNotNull(element, $"element '{automationId}' not found in window '{window.Title}'");
        Win32WindowFinder.ScrollIntoView(element!);
        element!.Click();
    }

    // ClientDetailsView/AccountDetailsView are wrapped in a ScrollViewer, and plain Button
    // controls don't support ScrollItemPattern - a button below the fold still resolves via UIA
    // tree query and reports a (stale, off-window) bounding rect, so a coordinate-based click
    // can silently hit nothing. WPF has no modal ShowDialog() calls anywhere in this client
    // (unlike WinForms, where a raw Invoke() would deadlock against the nested message loop a
    // modal dialog's click handler opens), so InvokePattern is safe here and sidesteps
    // scrolling/visibility/coordinates entirely.
    private static void InvokeById(Window window, string automationId)
    {
        var element = WaitForElement(window, automationId, 10);
        Assert.IsNotNull(element, $"element '{automationId}' not found in window '{window.Title}'");
        if (element!.Patterns.Invoke.IsSupported)
            element.Patterns.Invoke.Pattern.Invoke();
        else
        {
            Win32WindowFinder.ScrollIntoView(element);
            element.Click();
        }
    }

    private static void SetText(Window window, string automationId, string value)
    {
        var textBox = WaitForElement(window, automationId, 10)?.AsTextBox();
        Assert.IsNotNull(textBox, $"textbox '{automationId}' not found");
        textBox!.Text = value;
    }

    private static ListBoxItem? WaitForListItem(Window window, string listAutomationId, string text, int timeoutSeconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
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

    private static ListBoxItem? WaitForListItemContaining(Window window, string listAutomationId, string text, int timeoutSeconds) =>
        WaitForListItem(window, listAutomationId, text, timeoutSeconds);
}
