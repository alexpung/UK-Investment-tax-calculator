using Microsoft.Playwright;
using NUnit.Framework;

namespace PlaywrightTests;

/// <summary>
/// Verifies the home page footer shows the application version and build date,
/// so users can tell at a glance which build they are running.
/// </summary>
[TestFixture]
public class HomeFooterTests : PlaywrightTestBase
{
    [Test]
    public async Task HomePageShowsVersionAndBuildDate()
    {
        await NavigateAndWaitForBlazorAsync("/");

        var footer = Page.Locator("[data-testid='app-version']");
        await Expect(footer).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

        string text = await footer.InnerTextAsync();
        TestContext.WriteLine($"Footer text: {text}");
        Assert.That(text, Does.Match(@"Version \d+\.\d+\.\d+"), "Footer should show a semantic version");
        Assert.That(text, Does.Match(@"Built \d{4}-\d{2}-\d{2} \d{2}:\d{2} UTC"), "Footer should show the build date");
    }
}
