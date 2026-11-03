using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace PlaywrightTests;

/// <summary>
/// Base test class for Playwright tests.
/// Inherits from PageTest which provides browser and page lifecycle management.
/// </summary>
public class PlaywrightTestBase : PageTest
{
    /// <summary>
    /// Base URL for the application under test.
    /// Uses BASE_URL environment variable if set, otherwise defaults to localhost HTTP.
    /// </summary>
    protected string BaseUrl => Environment.GetEnvironmentVariable("BASE_URL") ?? "http://127.0.0.1:5000";

    /// <summary>
    /// Configure Playwright context to ignore HTTPS certificate errors for dev certs.
    /// The locale is pinned to a non-GBP one so formatting assertions do not depend on the
    /// locale of the machine running the tests, and so amounts that wrongly follow the browser
    /// culture instead of the base currency are caught.
    /// </summary>
    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            Locale = "en-US"
        };
    }

    /// <summary>
    /// Navigates to the specified path and waits for Blazor WebAssembly to fully hydrate.
    /// </summary>
    protected async Task NavigateAndWaitForBlazorAsync(string path = "/")
    {
        await Page.GotoAsync($"{BaseUrl}{path}", new PageGotoOptions 
        { 
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 60000 
        });
        
        // Wait for Blazor to finish loading - check for the sidebar nav which indicates full render
        await Page.WaitForSelectorAsync(".sidebar-nav", new PageWaitForSelectorOptions 
        { 
            State = WaitForSelectorState.Visible,
            Timeout = 30000
        });
        
        // Give Blazor a moment to finish any remaining hydration
        await Task.Delay(1000);
    }

    /// <summary>
    /// Waits for a workflow step in the sidebar to be visible. Steps are always expanded, so their
    /// links can be clicked straight away once the step is shown.
    /// </summary>
    protected async Task WaitForWorkflowStepAsync(string stepTitle)
    {
        var step = Page.Locator($".sidebar-nav .workflow-step:has(.workflow-title:has-text('{stepTitle}'))").First;
        await step.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    /// <summary>
    /// Clicks a sidebar link (client-side navigation, so imported data is kept) and waits for its route.
    /// </summary>
    protected async Task NavigateViaSidebarAsync(string linkText, string urlGlob)
    {
        var link = Page.Locator($".sidebar-nav .nav-link-custom:text-is('{linkText}')").First;
        await link.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await link.ClickAsync();
        await Page.WaitForURLAsync(urlGlob, new PageWaitForURLOptions { Timeout = 15000 });
    }

    /// <summary>
    /// Moves from the import page to the Calculate &amp; export page, keeping the imported data.
    /// </summary>
    protected Task GoToCalculatePageAsync() => NavigateViaSidebarAsync("Calculate & export", "**/CalculatePage");

    /// <summary>
    /// Waits for a specific visible text to appear on the page.
    /// </summary>
    protected async Task WaitForTextAsync(string text, int timeoutMs = 10000)
    {
        await Page.Locator($"text={text}").First.WaitForAsync(new LocatorWaitForOptions 
        { 
            State = WaitForSelectorState.Visible,
            Timeout = timeoutMs
        });
    }
}
