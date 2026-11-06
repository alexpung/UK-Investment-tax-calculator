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
        await Expect(Page.Locator(".sidebar-brand:has-text('UK Investment')")).ToBeVisibleAsync();
    }

    [Test]
    public async Task AllNavigationLinks_LoadSuccessfully()
    {
        // Test all navigation links in a single browser session for efficiency
        var linksToTest = new (string Path, string ExpectedText)[]
        {
            ("/MainCalculatorPage", "Import Data"),
            ("/CalculatePage", "Calculate Taxes"),
            ("/PdfReportPage", "PDF"),
            ("/CgtYearlyTaxSummaryPage", "CGT"),
            ("/DividendYearlyTaxSummaryPage", "Dividend"),
            ("/CalculationViewPage", "Disposal"),
            ("/TradeMatchPage", "Trade"),
            ("/ImportedTradesDataPage", "Imported Trades"),
            ("/DividendDataPage", "Dividend"),
            ("/Section104DataPage", "Section 104"),
            ("/AddTradePage", "Trade"),
            ("/ResidencyStatusPage", "Residency"),
            ("/AddExcessReportableIncome", "Reportable"),
            ("/corporate-actions", "Corporate"),
            ("/AcknowledgementPage", "Acknowledgement"),
            ("/HowItWorks", "How the calculator works"),
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

        // Follow the first two workflow steps
        await WaitForWorkflowStepAsync("Import");
        await NavigateViaSidebarAsync("Import data", "**/MainCalculatorPage");
        await WaitForTextAsync("Import Data");

        await NavigateViaSidebarAsync("Calculate & export", "**/CalculatePage");
        await WaitForTextAsync("Calculate Taxes");
    }

    [Test]
    public async Task WorkflowSteps_ShowEveryPageWithoutExpanding()
    {
        await NavigateAndWaitForBlazorAsync("/");

        var steps = Page.Locator(".sidebar-nav .workflow-step");
        Assert.That(await steps.CountAsync(), Is.EqualTo(6), "Sidebar should show the six workflow steps");

        string[] expectedLinks =
        [
            "Import data", "Calculate & export", "Income taxable dividend selection", "ERI and Equalisation",
            "Corporate Actions and Events", "Add Manual Entries", "Residency Status", "CGT Yearly Summary",
            "Dividend/Income Summary", "Disposal Summary", "Trade Matches", "Imported Trades",
            "Dividend/Income Data", "Section 104 Data", "PDF Report", "How it works", "Acknowledgement"
        ];
        foreach (var linkText in expectedLinks)
        {
            await Expect(Page.Locator($".sidebar-nav .nav-link-custom:text-is('{linkText}')")).ToBeVisibleAsync();
        }
    }

    [Test]
    public async Task WorkflowStep_IsMarkedCurrentForActivePage()
    {
        await NavigateAndWaitForBlazorAsync("/CgtYearlyTaxSummaryPage");

        var currentStep = Page.Locator(".sidebar-nav .workflow-step.is-current");
        await Expect(currentStep).ToHaveCountAsync(1);
        await Expect(currentStep.Locator(".workflow-title")).ToContainTextAsync("Review");
        await Expect(Page.Locator("[data-testid='workflow-progress']")).ToContainTextAsync("Step 4 of 6");
    }
}
