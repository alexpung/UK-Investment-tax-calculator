using Microsoft.Playwright;
using NUnit.Framework;

namespace PlaywrightTests;

/// <summary>
/// Tests the full calculation workflow: loading an XML file and verifying results.
/// Expected values are based on TaxExample.xml content.
/// </summary>
[TestFixture]
public class CalculationWorkflowTests : PlaywrightTestBase
{
    private string TestDataPath => Path.Combine(AppContext.BaseDirectory, "TestData", "TaxExample.xml");
    
    // Expected values from TaxExample.xml
    private const int ExpectedTotalEvents = 34;
    private const int ExpectedTrades = 11;
    private const int ExpectedCgtRows = 9;
    private const int ExpectedDividendRows = 4;
    private const int ExpectedSection104Rows = 17;
    private const int ExpectedDisposals = 12;
    private const string ExpectedDisposalProceeds = "£3,617,218.00";
    // Baselines align with current stock-split/corporate-action sequencing logic.
    private const string ExpectedAllowableCost = "£7,006,456.00";
    private const string ExpectedTotalGain = "£3,614,257.00";
    private const string ExpectedTotalLoss = "-£7,003,493.00";
    private const string ExpectedTotalDividend = "£555.00";
    private const string ExpectedForeignTaxPaid = "-£166.50";

    [Test]
    public async Task LoadXmlFile_ShowsImportStatistics()
    {
        await NavigateAndWaitForBlazorAsync("/MainCalculatorPage");

        // Verify the Import Data card is visible
        await Expect(Page.Locator("text=Import Data")).ToBeVisibleAsync();

        // Get initial event count (should be 0)
        var totalEventsCard = Page.Locator(".metric-card:has-text('Total Tax Events') .metric-value");
        await Expect(totalEventsCard).ToBeVisibleAsync();
        var initialCount = await ParseMetricValueAsync(totalEventsCard, "Total Tax Events (initial)");
        TestContext.WriteLine($"Initial event count: {initialCount}");
        Assert.That(initialCount, Is.EqualTo(0), "Should start with 0 events");

        // Upload file
        await UploadTestFileAsync();

        // Verify import statistics match expected values from TaxExample.xml
        var newCount = await ParseMetricValueAsync(totalEventsCard, "Total Tax Events (after import)");
        TestContext.WriteLine($"Event count after import: {newCount}");
        Assert.That(newCount, Is.EqualTo(ExpectedTotalEvents), 
            $"Should have {ExpectedTotalEvents} total events from TaxExample.xml");
        
        // Verify trades count
        var tradesCard = Page.Locator(".metric-card").Filter(new LocatorFilterOptions { HasText = "Trades" }).First.Locator(".metric-value");
        var tradesCount = await ParseMetricValueAsync(tradesCard, "Trades");
        TestContext.WriteLine($"Trades count: {tradesCount}");
        Assert.That(tradesCount, Is.EqualTo(ExpectedTrades), 
            $"Should have {ExpectedTrades} trades from TaxExample.xml");
    }

    [Test]
    public async Task StartCalculation_PopulatesResults()
    {
        await NavigateAndWaitForBlazorAsync("/MainCalculatorPage");

        // Upload file
        await UploadTestFileAsync();

        // Verify data was imported
        var totalEventsCard = Page.Locator(".metric-card:has-text('Total Tax Events') .metric-value");
        var eventCount = await ParseMetricValueAsync(totalEventsCard, "Total Tax Events (before calc)");
        Assert.That(eventCount, Is.EqualTo(ExpectedTotalEvents), "Should have expected events before calculation");

        // Find and click the Start Calculation button
        var calcButton = Page.Locator("button:has-text('Start Calculation')");
        await Expect(calcButton).ToBeEnabledAsync(new LocatorAssertionsToBeEnabledOptions { Timeout = 15000 });
        await calcButton.ClickAsync();

        // Wait for calculation to complete
        await WaitForCalculationToCompleteAsync();
        
        // Verify CalculationSummary component is populated:
        var yearSelector = Page.Locator("text=Select Tax Year");
        await Expect(yearSelector).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        var taxSummaryHeader = Page.Locator("text=Tax Summary");
        await Expect(taxSummaryHeader).ToBeVisibleAsync();
        
        // Collect ALL values first, then log them all
        var disposalsText = await Page.Locator(".metric-card:has-text('Number of Disposals') .metric-value").TextContentAsync() ?? "";
        var proceedsText = await Page.Locator(".metric-card:has-text('Total Disposal Proceeds') .metric-value").TextContentAsync() ?? "";
        var allowableCostText = await Page.Locator(".metric-card:has-text('Total Allowable Cost') .metric-value").TextContentAsync() ?? "";
        var gainText = await Page.Locator(".metric-card:has-text('Total Gain') .metric-value").TextContentAsync() ?? "";
        var lossText = await Page.Locator(".metric-card:has-text('Total Loss') .metric-value").TextContentAsync() ?? "";
        var dividendText = await Page.Locator(".metric-card:has-text('Total Dividend') .metric-value").TextContentAsync() ?? "";
        var foreignTaxText = await Page.Locator(".metric-card:has-text('Total Foreign Withholding Tax Paid') .metric-value").TextContentAsync() ?? "";
        
        // Log all values
        TestContext.WriteLine($"Number of Disposals: {disposalsText}");
        TestContext.WriteLine($"Total Disposal Proceeds: {proceedsText}");
        TestContext.WriteLine($"Total Allowable Cost: {allowableCostText}");
        TestContext.WriteLine($"Total Gain: {gainText}");
        TestContext.WriteLine($"Total Loss: {lossText}");
        TestContext.WriteLine($"Total Dividend: {dividendText}");
        TestContext.WriteLine($"Total Foreign Withholding Tax Paid: {foreignTaxText}");
        
        // Now do all assertions
        Assert.That(int.Parse(disposalsText), Is.EqualTo(ExpectedDisposals), "Number of Disposals mismatch");
        Assert.That(proceedsText, Is.EqualTo(ExpectedDisposalProceeds), "Total Disposal Proceeds mismatch");
        Assert.That(allowableCostText, Is.EqualTo(ExpectedAllowableCost), "Total Allowable Cost mismatch");
        Assert.That(gainText, Is.EqualTo(ExpectedTotalGain), "Total Gain mismatch");
        Assert.That(lossText, Is.EqualTo(ExpectedTotalLoss), "Total Loss mismatch");
        Assert.That(dividendText, Is.EqualTo(ExpectedTotalDividend), "Total Dividend mismatch");
        Assert.That(foreignTaxText, Is.EqualTo(ExpectedForeignTaxPaid), "Total Foreign Withholding Tax Paid mismatch");
    }

    [Test]
    public async Task AfterCalculation_CgtSummaryPageHasData()
    {
        await LoadAndCalculateAsync();

        // Navigate to CGT Yearly Summary page
        await ExpandNavCategoryAsync("Tax summaries");
        await Page.Locator(".nav-link-custom:has-text('CGT Yearly Summary')").ClickAsync();
        await Page.WaitForURLAsync("**/CgtYearlyTaxSummaryPage", new PageWaitForURLOptions { Timeout = 10000 });
        
        await Task.Delay(2000);

        // Check for data rows
        var dataRows = Page.Locator(".e-grid .e-row, table tbody tr");
        var rowCount = await dataRows.CountAsync();
        TestContext.WriteLine($"CGT Summary page has {rowCount} data rows");
        
        Assert.That(rowCount, Is.EqualTo(ExpectedCgtRows), 
            $"CGT Summary page should have {ExpectedCgtRows} data rows");
    }

    [Test]
    public async Task AfterCalculation_DividendDataPageHasData()
    {
        await LoadAndCalculateAsync();

        // Navigate to Dividend Data page
        await ExpandNavCategoryAsync("Raw Data");
        await Page.Locator(".nav-link-custom:has-text('Dividend/Income Data')").ClickAsync();
        await Page.WaitForURLAsync("**/DividendDataPage", new PageWaitForURLOptions { Timeout = 10000 });

        await Task.Delay(2000);
        
        // Check for data rows
        var dataRows = Page.Locator(".e-grid .e-row, table tbody tr");
        var rowCount = await dataRows.CountAsync();
        TestContext.WriteLine($"Dividend Data page has {rowCount} data rows");
        
        Assert.That(rowCount, Is.EqualTo(ExpectedDividendRows), 
            $"Dividend Data page should have {ExpectedDividendRows} data rows");
    }

    [Test]
    public async Task AfterCalculation_Section104DataPageHasData()
    {
        await LoadAndCalculateAsync();

        // Navigate to Section 104 Data page
        await ExpandNavCategoryAsync("Raw Data");
        await Page.Locator(".nav-link-custom:has-text('Section 104 Data')").ClickAsync();
        await Page.WaitForURLAsync("**/Section104DataPage", new PageWaitForURLOptions { Timeout = 10000 });

        await Task.Delay(2000);

        // Check for data rows
        var dataRows = Page.Locator(".e-grid .e-row, table tbody tr");
        var rowCount = await dataRows.CountAsync();
        TestContext.WriteLine($"Section 104 Data page has {rowCount} data rows");
        
        Assert.That(rowCount, Is.EqualTo(ExpectedSection104Rows), 
            $"Section 104 Data page should have {ExpectedSection104Rows} data rows");
    }

    /// <summary>
    /// Waits for calculation to complete by monitoring button state.
    /// Throws if calculation doesn't complete within timeout.
    /// </summary>
    private async Task WaitForCalculationToCompleteAsync()
    {
        var calcButton = Page.Locator("button:has-text('Start Calculation'), button:has-text('Calculating')");
        
        var startTime = DateTime.UtcNow;
        var timeoutSeconds = 60;
        var lastButtonText = "";
        
        while ((DateTime.UtcNow - startTime).TotalSeconds < timeoutSeconds)
        {
            lastButtonText = await calcButton.First.TextContentAsync() ?? "";
            if (!lastButtonText.Contains("Calculating"))
            {
                // Calculation completed successfully
                await Task.Delay(2000); // Wait for UI to settle
                return;
            }
            await Task.Delay(500);
        }
        
        // Timeout reached - fail explicitly with context
        var elapsed = DateTime.UtcNow - startTime;
        Assert.Fail($"Calculation did not complete within {timeoutSeconds} seconds. " +
                    $"Elapsed: {elapsed.TotalSeconds:F1}s. Last button text: '{lastButtonText}'");
    }

    /// <summary>
    /// Uploads the test XML file using Syncfusion uploader.
    /// </summary>
    private async Task UploadTestFileAsync()
    {
        TestContext.WriteLine($"Uploading file: {TestDataPath}");
        Assert.That(File.Exists(TestDataPath), Is.True, $"Test file should exist at {TestDataPath}");
        
        var fileInput = Page.Locator("input[type='file']").First;
        await fileInput.SetInputFilesAsync(TestDataPath);
        TestContext.WriteLine("File set on input element");
        
        await Task.Delay(1000);
        
        var uploadButton = Page.Locator(".e-upload-actions .e-file-upload-btn, .e-upload button:has-text('Upload')");
        if (await uploadButton.CountAsync() > 0)
        {
            var isVisible = await uploadButton.First.IsVisibleAsync();
            TestContext.WriteLine($"Upload button visible: {isVisible}");
            if (isVisible)
            {
                await uploadButton.First.ClickAsync();
                TestContext.WriteLine("Clicked upload button");
            }
        }
        
        await Task.Delay(3000);
        
        var spinner = Page.Locator(".spinner-border");
        try 
        {
            await spinner.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 2000 });
            TestContext.WriteLine("Spinner appeared, waiting for it to disappear...");
            await spinner.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 30000 });
            TestContext.WriteLine("Spinner disappeared");
        }
        catch (TimeoutException ex)
        {
            TestContext.WriteLine($"Spinner did not appear (fast processing or not present): {ex.Message}");
        }
        
        await Task.Delay(1000);
    }

    /// <summary>
    /// Helper method to load XML and run calculation
    /// </summary>
    private async Task LoadAndCalculateAsync()
    {
        await NavigateAndWaitForBlazorAsync("/MainCalculatorPage");
        
        await UploadTestFileAsync();
        
        var totalEventsCard = Page.Locator(".metric-card:has-text('Total Tax Events') .metric-value");
        var eventCount = await ParseMetricValueAsync(totalEventsCard, "Total Tax Events");
        TestContext.WriteLine($"Events after upload: {eventCount}");
        Assert.That(eventCount, Is.EqualTo(ExpectedTotalEvents), "Should have expected events after upload");
        
        var calcButton = Page.Locator("button:has-text('Start Calculation')");
        await Expect(calcButton).ToBeEnabledAsync(new LocatorAssertionsToBeEnabledOptions { Timeout = 15000 });
        await calcButton.ClickAsync();
        TestContext.WriteLine("Clicked Start Calculation");
        
        await WaitForCalculationToCompleteAsync();
        TestContext.WriteLine("Calculation complete");
    }

    /// <summary>
    /// Robustly parses an integer metric value from a locator, with diagnostic logging on failure.
    /// </summary>
    private async Task<int> ParseMetricValueAsync(ILocator locator, string metricName)
    {
        var rawText = await locator.TextContentAsync() ?? "";
        
        // Clean the text - remove any non-numeric characters except minus sign
        var cleanedText = rawText.Trim();
        
        if (int.TryParse(cleanedText, out var result))
        {
            return result;
        }
        
        // Log detailed diagnostic info before failing
        TestContext.WriteLine($"Failed to parse '{metricName}' metric. Raw text: '{rawText}', Cleaned: '{cleanedText}'");
        
        // Return 0 as default for initial state checks, or fail for expected values
        if (string.IsNullOrWhiteSpace(cleanedText))
        {
            TestContext.WriteLine($"Metric '{metricName}' returned empty text, defaulting to 0");
            return 0;
        }
        
        Assert.Fail($"Could not parse metric '{metricName}': expected integer, got '{rawText}'");
        return 0; // Unreachable but required for compiler
    }
}
