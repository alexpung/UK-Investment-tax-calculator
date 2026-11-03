using Microsoft.Playwright;
using NUnit.Framework;

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
}
