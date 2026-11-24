using Microsoft.Playwright;
using NUnit.Framework;

using System.Text.RegularExpressions;

namespace PlaywrightTests;

/// <summary>
/// The Income Taxable Dividend Selection page: pick a dividend-paying ticker and convert it to income.
/// </summary>
[TestFixture]
public class IncomeTaxableDividendSelectionTests : PlaywrightTestBase
{
    private string TestDataPath => Path.Combine(AppContext.BaseDirectory, "TestData", "TaxExample.xml");

    [Test]
    public async Task SelectedTicker_IsConvertedAndListedAsIncome()
    {
        await NavigateAndWaitForBlazorAsync("/MainCalculatorPage");
        await Page.Locator("input[type='file']").First.SetInputFilesAsync(TestDataPath);
        await Expect(Page.Locator(".metric-card:has-text('Total Tax Events') .metric-value")).Not.ToHaveTextAsync("0",
            new LocatorAssertionsToHaveTextOptions { Timeout = 30000 });

        await NavigateViaSidebarAsync("Income taxable dividend selection", "**/ConvertPage");

        var tickers = Page.Locator("[data-testid='dividend-tickers'] .convert-ticker");
        await Expect(tickers.First).ToBeVisibleAsync();
        int tickerCount = await tickers.CountAsync();
        string ticker = (await tickers.First.InnerTextAsync()).Trim();
        await Expect(Page.Locator("[data-testid='converted-tickers']")).ToContainTextAsync("No tickers have been converted yet.");

        // Search narrows the list to the ticker
        await Page.Locator(".convert-search input").FillAsync(ticker);
        await Expect(tickers.Filter(new LocatorFilterOptions { HasText = ticker })).ToHaveCountAsync(1);

        await tickers.Filter(new LocatorFilterOptions { HasText = ticker }).Locator("input").CheckAsync();
        await Expect(Page.Locator(".convert-actions")).ToContainTextAsync("1 selected");

        await Page.Locator(".convert-buttons .btn-primary").ClickAsync();

        await Expect(Page.Locator("[data-testid='converted-tickers'] tr.rz-data-row")).ToContainTextAsync(ticker);
        if (tickerCount == 1)
        {
            // The only dividend-paying ticker was converted, so nothing is left to select
            await Expect(Page.Locator("[data-testid='all-converted']")).ToBeVisibleAsync();
        }
        else
        {
            await Page.Locator(".convert-search input").FillAsync("");
            await Expect(tickers).ToHaveCountAsync(tickerCount - 1);
            await Expect(Page.Locator(".convert-actions")).ToContainTextAsync("No tickers selected");
        }
    }

    [Test]
    public async Task ConvertingAfterCalculating_RecalculatesWhenLeavingThePage()
    {
        // TaxExample.xml: £555.00 of dividends with -£166.50 withholding tax, no interest income
        await NavigateAndWaitForBlazorAsync("/MainCalculatorPage");
        await Page.Locator("input[type='file']").First.SetInputFilesAsync(TestDataPath);
        await Expect(Page.Locator(".metric-card:has-text('Total Tax Events') .metric-value")).Not.ToHaveTextAsync("0",
            new LocatorAssertionsToHaveTextOptions { Timeout = 30000 });
        await GoToCalculatePageAsync();
        await Page.Locator("button:has-text('Start Calculation')").ClickAsync();
        await Expect(Page.Locator("text=Calculation completed").First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60000 });

        var totals = Page.Locator(".rz-grid-table tfoot").First;
        await NavigateViaSidebarAsync("Dividend/Income Summary", "**/DividendYearlyTaxSummaryPage");
        // Footer totals in column order: interest income, taxable dividend, foreign tax paid
        await Expect(totals).ToHaveTextAsync(FooterTotals("£0.00", "£555.00", "-£166.50"));

        await NavigateViaSidebarAsync("Income taxable dividend selection", "**/ConvertPage");
        await Page.Locator("[data-testid='dividend-tickers'] .convert-ticker input").First.CheckAsync();
        await Page.Locator(".convert-buttons .btn-primary").ClickAsync();
        await Expect(Page.Locator("[data-testid='converted-tickers'] tr.rz-data-row")).ToHaveCountAsync(1);

        // Leaving the page re-runs the calculation: the dividends are now taxed as interest income, while the
        // withholding tax stays a dividend entry and is still counted as foreign tax paid
        await NavigateViaSidebarAsync("Dividend/Income Summary", "**/DividendYearlyTaxSummaryPage");
        await Expect(totals).ToHaveTextAsync(FooterTotals("£555.00", "£0.00", "-£166.50"),
            new LocatorAssertionsToHaveTextOptions { Timeout = 30000 });
    }

    private static Regex FooterTotals(string interest, string dividend, string foreignTax) =>
        new($@"Total:\s*{Regex.Escape(interest)}\s*Total:\s*{Regex.Escape(dividend)}\s*Total:\s*{Regex.Escape(foreignTax)}");
}
