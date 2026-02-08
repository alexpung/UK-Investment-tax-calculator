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
    /// Uses BASE_URL environment variable if set, otherwise defaults to localhost.
    /// </summary>
    protected string BaseUrl => Environment.GetEnvironmentVariable("BASE_URL") ?? "https://localhost:5001";

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
    /// Expands a navigation category by clicking its header.
    /// </summary>
    protected async Task ExpandNavCategoryAsync(string categoryTitle)
    {
        var categoryButton = Page.Locator($".category-header:has-text('{categoryTitle}')");
        await categoryButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        
        var isExpanded = await categoryButton.GetAttributeAsync("class");
        if (isExpanded?.Contains("expanded") != true)
        {
            await categoryButton.ClickAsync();
            // Wait for animation and content to render
            await Task.Delay(500);
        }
    }

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
