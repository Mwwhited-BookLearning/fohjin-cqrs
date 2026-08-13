using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fohjin.DDD.BankApplication.UITests;

// Drives the real Vue dev server (Fohjin.DDD.WebUI) through a full sign-in/sign-out round trip
// against the real Fohjin.DDD.Sts and Fohjin.DDD.WebApi - added alongside the fix for a real bug
// (docs/00-architecture-overview.md's Observability section, CLAUDE.md's bug list): Fohjin.DDD.Sts
// never registered a PostLogoutRedirectUris/EndSession permission for the dev client, so OpenIddict
// rejected every post_logout_redirect_uri a caller sent (error ID2052), and the sign-out
// confirmation's SignOut() call fell back to Fohjin.DDD.Sts's own generic MVC home page instead of
// returning to the calling app. A screenshot at each step makes a regression in either direction
// (broken sign-out, or a fix that only silences the error without actually returning to the caller)
// visible without needing another manual live-verification pass to notice.
[TestClass]
[TestCategory("ui")]
public class VueSignInSignOutTest
{
    private static VueAppFixture _fixture = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static async Task ClassSetup(TestContext _)
    {
        _fixture = new VueAppFixture();
        await _fixture.StartAsync();
    }

    [ClassCleanup]
    public static async Task ClassTeardown()
    {
        await _fixture.DisposeAsync();
    }

    [TestMethod]
    public async Task Signing_in_and_out_returns_to_the_Vue_app_not_the_Sts_home_page()
    {
        var page = _fixture.Page;
        var screenshotsDir = Path.Combine(TestContext.TestResultsDirectory ?? Path.GetTempPath(), "screenshots");
        Directory.CreateDirectory(screenshotsDir);

        async Task Shot(string name)
        {
            var path = Path.Combine(screenshotsDir, name);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = path });
            TestContext.AddResultFile(path);
        }

        await page.GotoAsync("http://localhost:5173/");
        Assert.IsNotNull(await page.WaitForSelectorAsync("text=Sign in", new PageWaitForSelectorOptions { Timeout = 15000 }));
        await Shot("01-login-page.png");

        await page.ClickAsync("text=Sign in");
        await page.WaitForURLAsync("**://127.0.0.1:5310/**", new PageWaitForURLOptions { Timeout = 15000 });
        Assert.IsNotNull(await page.WaitForSelectorAsync("#Input_Email", new PageWaitForSelectorOptions { Timeout = 15000 }));
        await Shot("02-sts-login-page.png");

        await page.FillAsync("#Input_Email", "dev@fohjin.local");
        await page.FillAsync("#Input_Password", "Dev!Passw0rd");
        await page.ClickAsync("#login-submit");

        await page.WaitForURLAsync("**://localhost:5173/**", new PageWaitForURLOptions { Timeout = 20000 });
        Assert.IsNotNull(await page.WaitForSelectorAsync("text=Sign out", new PageWaitForSelectorOptions { Timeout = 15000 }));
        await Shot("03-signed-in-clients-page.png");

        await page.ClickAsync("text=Sign out");
        await page.WaitForURLAsync("**://127.0.0.1:5310/connect/logout**", new PageWaitForURLOptions { Timeout = 15000 });
        Assert.IsNotNull(await page.WaitForSelectorAsync(
            "text=Are you sure you want to sign out?", new PageWaitForSelectorOptions { Timeout = 15000 }));
        await Shot("04-sts-logout-confirmation.png");

        await page.ClickAsync("input[type=submit][value=Yes]");
        await page.WaitForURLAsync("**://localhost:5173/**", new PageWaitForURLOptions { Timeout = 15000 });
        Assert.IsNotNull(await page.WaitForSelectorAsync("text=Sign in", new PageWaitForSelectorOptions { Timeout = 15000 }));
        await Shot("05-signed-out-back-in-vue-app.png");

        // The bug this test guards against: a rejected post_logout_redirect_uri makes
        // AuthorizationController.LogoutPost() fall back to Fohjin.DDD.Sts's own "/" - this
        // assertion is what actually catches that, not just "an error didn't happen".
        StringAssert.StartsWith(page.Url, "http://localhost:5173");
    }
}
