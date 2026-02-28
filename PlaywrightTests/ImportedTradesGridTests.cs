using Microsoft.Playwright;
using NUnit.Framework;

namespace PlaywrightTests;

[TestFixture]
public class ImportedTradesGridTests : PlaywrightTestBase
{
    private string TestDataPath => Path.Combine(AppContext.BaseDirectory, "TestData", "TaxExample.xml");

    [Test]
    public async Task ImportedTradesGrid_ShowsOptionAndFutureRows_AndSupportsDelete()
    {
        await NavigateAndWaitForBlazorAsync("/MainCalculatorPage");
        await UploadTestFileAsync();

        await ExpandNavCategoryAsync("Raw Data");
        await Page.Locator(".nav-link-custom:has-text('Imported Trades')").ClickAsync();
        await Page.WaitForURLAsync("**/ImportedTradesDataPage", new PageWaitForURLOptions { Timeout = 10000 });
        await Page.WaitForSelectorAsync("#ImportedTradesGrid", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 15000
        });

        var rowKeysBeforeDelete = await GetRenderedRowKeysAsync();
        Assert.That(rowKeysBeforeDelete.Count, Is.GreaterThan(0), "Expected imported trades grid to have rows.");

        var visibleAssetTypes = await GetRenderedAssetTypesAsync();
        Assert.That(visibleAssetTypes.Any(type => type.Equals("OPTION", StringComparison.OrdinalIgnoreCase)),
            Is.True, "Expected OPTION rows to be visible in imported trades grid.");
        Assert.That(visibleAssetTypes.Any(type => type.Equals("FUTURE", StringComparison.OrdinalIgnoreCase)),
            Is.True, "Expected FUTURE rows to be visible in imported trades grid.");

        var firstRow = Page.Locator("#ImportedTradesGrid .e-gridcontent tr.e-row").First;
        var firstRowKey = (await firstRow.InnerTextAsync())?.Trim() ?? string.Empty;
        Assert.That(firstRowKey, Is.Not.Empty, "Could not capture a row key before delete.");

        await firstRow.Locator("button:has-text('Delete')").ClickAsync();
        await Task.Delay(1200);

        var rowKeysAfterDelete = await GetRenderedRowKeysAsync();
        Assert.That(rowKeysAfterDelete.Contains(firstRowKey), Is.False,
            $"Deleted row should no longer be visible. Deleted row key: {firstRowKey}");
    }

    private async Task<List<string>> GetRenderedRowKeysAsync()
    {
        return (await Page.Locator("#ImportedTradesGrid .e-gridcontent tr.e-row").AllInnerTextsAsync())
            .Select(text => text.Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();
    }

    private async Task<List<string>> GetRenderedAssetTypesAsync()
    {
        var values = await Page.EvaluateAsync<string[]>(@"() => {
            const gridEl = document.getElementById('ImportedTradesGrid');
            if (!gridEl) return [];
            const headerCells = Array.from(gridEl.querySelectorAll('.e-headercontent th.e-headercell'));
            const assetTypeIndex = headerCells.findIndex(h => ((h.textContent || '').trim().toLowerCase().includes('asset type')));
            if (assetTypeIndex < 0) return [];

            const rows = Array.from(gridEl.querySelectorAll('.e-gridcontent tr.e-row'));
            return rows
                .map(row => {
                    const cells = Array.from(row.querySelectorAll('td.e-rowcell'));
                    if (assetTypeIndex >= cells.length) return '';
                    return (cells[assetTypeIndex].textContent || '').trim();
                })
                .filter(v => v.length > 0);
        }");
        return values.ToList();
    }

    private async Task UploadTestFileAsync()
    {
        Assert.That(File.Exists(TestDataPath), Is.True, $"Test file should exist at {TestDataPath}");

        var fileInput = Page.Locator("input[type='file']").First;
        await fileInput.SetInputFilesAsync(TestDataPath);
        await Task.Delay(1000);

        var uploadButton = Page.Locator(".e-upload-actions .e-file-upload-btn, .e-upload button:has-text('Upload')");
        if (await uploadButton.CountAsync() > 0 && await uploadButton.First.IsVisibleAsync())
        {
            await uploadButton.First.ClickAsync();
        }

        var duplicateWarning = Page.Locator("text=Duplicate Import Warning");
        try
        {
            await duplicateWarning.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 1500 });
            await Page.Locator("button:has-text('Skip Duplicates')").ClickAsync();
        }
        catch (TimeoutException)
        {
            // Duplicate warning not shown, continue.
        }

        var spinner = Page.Locator(".spinner-border");
        try
        {
            await spinner.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 2000 });
            await spinner.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 30000 });
        }
        catch (TimeoutException)
        {
            // Some runs finish before spinner becomes visible.
        }

        await Task.Delay(1000);
    }
}
