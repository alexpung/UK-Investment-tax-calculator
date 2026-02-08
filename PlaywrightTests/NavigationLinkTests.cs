using Microsoft.Playwright;
using NUnit.Framework;

namespace PlaywrightTests;

/// <summary>
/// Tests that all navigation menu links are accessible and load correctly.
/// </summary>
[TestFixture]
public class NavigationLinkTests : PlaywrightTestBase
{
    [Test]
    public async Task HomePage_LoadsSuccessfully()
    {
        await NavigateAndWaitForBlazorAsync("/");
        
        // Verify the home page loads with the sidebar navigation
        await Expect(Page.Locator(".sidebar-nav")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=UK Tax Calculator")).ToBeVisibleAsync();
    }

    [Test]
    public async Task AllNavigationLinks_LoadSuccessfully()
    {
        // Test all navigation links in a single browser session for efficiency
        var linksToTest = new (string Path, string ExpectedText)[]
        {
            ("/MainCalculatorPage", "Import Data"),
            ("/PdfReportPage", "PDF"),
            ("/CgtYearlyTaxSummaryPage", "CGT"),
            ("/DividendYearlyTaxSummaryPage", "Dividend"),
            ("/CalculationViewPage", "Disposal"),
            ("/TradeMatchPage", "Trade"),
            ("/DividendDataPage", "Dividend"),
            ("/Section104DataPage", "Section 104"),
            ("/AddTradePage", "Trade"),
            ("/ResidencyStatusPage", "Residency"),
            ("/AddExcessReportableIncome", "Reportable"),
            ("/corporate-actions", "Corporate"),
            ("/AcknowledgementPage", "Acknowledgement"),
            ("/ConvertPage", "dividend")
        };

        var failures = new List<string>();

        foreach (var (path, expectedText) in linksToTest)
        {
            try
            {
                await Page.GotoAsync($"{BaseUrl}{path}", new PageGotoOptions 
                { 
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 30000 
                });
                
                // Wait for sidebar to confirm Blazor is loaded
                await Page.WaitForSelectorAsync(".sidebar-nav", new PageWaitForSelectorOptions 
                { 
                    State = WaitForSelectorState.Visible,
                    Timeout = 15000
                });
                
                // Check for expected text
                var textLocator = Page.GetByText(expectedText).First;
                var isVisible = await textLocator.IsVisibleAsync();
                
                if (!isVisible)
                {
                    failures.Add($"Page {path} should contain '{expectedText}' but text was not found");
                }
            }
            catch (Exception ex)
            {
                failures.Add($"Page {path} failed: {ex.Message}");
            }
        }

        if (failures.Count > 0)
        {
            Assert.Fail($"Navigation link failures:\n{string.Join("\n", failures)}");
        }
    }

    [Test]
    public async Task AllNavMenuLinks_AreClickable()
    {
        await NavigateAndWaitForBlazorAsync("/");

        // Check sidebar is visible
        await Expect(Page.Locator(".sidebar-nav")).ToBeVisibleAsync();

        // Get all nav links in sidebar
        var navLinks = Page.Locator(".sidebar-nav .nav-link-custom");
        var count = await navLinks.CountAsync();

        Assert.That(count, Is.GreaterThan(0), "Should have navigation links in sidebar");

        // Click Home link and verify navigation
        var homeLink = Page.Locator(".nav-link-custom:has-text('Home')");
        await Expect(homeLink).ToBeVisibleAsync();
        await homeLink.ClickAsync();
        await Page.WaitForURLAsync("**/");

        // Expand Import + PDF category and test a link
        await ExpandNavCategoryAsync("Import + PDF");
        var importLink = Page.Locator(".nav-link-custom:has-text('Import/Export')");
        await Expect(importLink).ToBeVisibleAsync();
        await importLink.ClickAsync();
        await Page.WaitForURLAsync("**/MainCalculatorPage");
        
        // Verify we navigated successfully - wait for page content
        await WaitForTextAsync("Import Data");
    }

    [Test]
    public async Task NavMenuCategories_ExpandAndCollapse()
    {
        await NavigateAndWaitForBlazorAsync("/");

        // Get the category content element that shows/hides
        var categoryContent = Page.Locator(".category-header:has-text('Tax summaries') + .category-content");
        
        // Initially should not have 'show' class
        var initialClasses = await categoryContent.GetAttributeAsync("class") ?? "";
        var initiallyExpanded = initialClasses.Contains("show");
        Assert.That(initiallyExpanded, Is.False, "Category should be initially collapsed");

        // Test expanding - click the category header
        var categoryHeader = Page.Locator(".category-header:has-text('Tax summaries')");
        await Expect(categoryHeader).ToBeVisibleAsync();
        await categoryHeader.ClickAsync();
        
        // Wait for expand animation
        await Task.Delay(500);
        
        // Verify category expanded - check for 'show' class on content
        var expandedClasses = await categoryContent.GetAttributeAsync("class") ?? "";
        Assert.That(expandedClasses.Contains("show"), Is.True, "Category content should have 'show' class when expanded");

        // Click again to collapse
        await categoryHeader.ClickAsync();
        await Task.Delay(500);
        
        // Verify category collapsed - 'show' class should be removed
        var collapsedClasses = await categoryContent.GetAttributeAsync("class") ?? "";
        Assert.That(collapsedClasses.Contains("show"), Is.False, "Category content should not have 'show' class when collapsed");
    }
}
