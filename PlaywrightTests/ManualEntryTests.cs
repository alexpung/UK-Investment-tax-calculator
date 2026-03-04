using Microsoft.Playwright;
using NUnit.Framework;

namespace PlaywrightTests;

/// <summary>
/// Tests that each manual entry type can be successfully added via the Add Manual Entries page.
/// Each test navigates to /AddTradePage, fills the form, clicks Add, and verifies the entry appears in the grid.
/// </summary>
[TestFixture]
public class ManualEntryTests : PlaywrightTestBase
{
    private const string PagePath = "/AddTradePage";

    [Test]
    public async Task CanAddStockTrade()
    {
        await NavigateAndWaitForBlazorAsync(PagePath);
        await WaitForTextAsync("Add Stock Trade");

        await FillComboBox("Enter ticker or asset name", "TESTSTOCK");
        await FillNumericByLabel("Quantity", "10");
        await FillMoneyAmountByTitle("Gross Proceed", "1000");

        await ClickButtonByText("Add Stock Trade");
        await VerifySuccessToast("added");
        await VerifyGridRowCount(1);
        await VerifyGridContainsText("TESTSTOCK");
    }

    [Test]
    public async Task CanAddOptionTrade()
    {
        await NavigateAndWaitForBlazorAsync(PagePath);
        await SelectEntryType("Option Trade");
        await WaitForTextAsync("Add Option Trade");

        await FillFirstComboBox("AAPL C 200 TEST");
        await FillTextByLabel("Underlying Asset", "AAPL");
        await FillNumericByLabel("Quantity (contracts)", "5");
        await FillMoneyAmountByTitle("Premium", "500");

        await ClickButtonByText("Add Option Trade");
        await VerifySuccessToast("added");
        await VerifyGridRowCount(1);
        await VerifyGridContainsText("Option Trade");
    }

    [Test]
    public async Task CanAddFutureContract()
    {
        await NavigateAndWaitForBlazorAsync(PagePath);
        await SelectEntryType("Future Contract");
        await WaitForTextAsync("Add Future Contract Trade");

        await FillComboBox("Enter ticker or asset name", "ES-TEST");
        await FillMoneyAmountByTitle("Contract Value", "5000");

        await ClickButtonByText("Add Future Trade");
        await VerifySuccessToast("added");
        await VerifyGridRowCount(1);
        await VerifyGridContainsText("Future Contract");
    }

    [Test]
    public async Task CanAddFxTrade()
    {
        await NavigateAndWaitForBlazorAsync(PagePath);
        await SelectEntryType("FX Trade");
        await WaitForTextAsync("Add FX Trade");

        // FX Trade defaults asset name to "USD"
        await FillNumericByLabel("Quantity", "1000");
        await FillMoneyAmountByTitle("Gross Proceed", "800");

        await ClickButtonByText("Add FX Trade");
        await VerifySuccessToast("added");
        await VerifyGridRowCount(1);
        await VerifyGridContainsText("FX Trade");
    }

    [Test]
    public async Task CanAddDividend()
    {
        await NavigateAndWaitForBlazorAsync(PagePath);
        await SelectEntryType("Dividend");
        await WaitForTextAsync("Add Dividend");

        await FillComboBox("Enter ticker or asset name", "MSFT-TEST");
        await FillMoneyAmountByTitle("Amount", "50");

        await ClickButtonByText("Add Dividend");
        await VerifySuccessToast("added");
        await VerifyGridRowCount(1);
        await VerifyGridContainsText("MSFT-TEST");
    }

    [Test]
    public async Task CanAddInterestIncome()
    {
        await NavigateAndWaitForBlazorAsync(PagePath);
        await SelectEntryType("Interest Income");
        await WaitForTextAsync("Add Interest Income");

        await FillComboBox("Enter ticker or asset name", "SAVINGS-TEST");
        await FillMoneyAmountByTitle("Amount", "100");

        await ClickButtonByText("Add Interest Income");
        await VerifySuccessToast("added");
        await VerifyGridRowCount(1);
        await VerifyGridContainsText("SAVINGS-TEST");
    }

    // ───────────────────── Helper methods ─────────────────────

    /// <summary>
    /// Selects an entry type from the page-level Syncfusion DropDownList.
    /// </summary>
    private async Task SelectEntryType(string entryTypeName)
    {
        // The entry type dropdown is inside .card.bg-dark
        var dropdown = Page.Locator(".card.bg-dark .e-ddl");
        await dropdown.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await dropdown.ClickAsync();

        // Wait for popup and click the item
        var listItem = Page.Locator($".e-dropdownbase .e-list-item:has-text('{entryTypeName}')").First;
        await listItem.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await listItem.ClickAsync();
    }

    /// <summary>
    /// Fills the first Syncfusion ComboBox on the form card (for asset name).
    /// Works regardless of placeholder text variations across entry types.
    /// </summary>
    private async Task FillFirstComboBox(string value)
    {
        var input = Page.Locator(".card.bg-secondary input.e-combobox").First;
        await input.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await input.ClickAsync();
        await input.FillAsync(value);
        await input.PressAsync("Tab");
    }

    /// <summary>
    /// Fills a Syncfusion ComboBox identified by its placeholder text.
    /// </summary>
    private async Task FillComboBox(string placeholder, string value)
    {
        var input = Page.Locator($"input.e-combobox[placeholder='{placeholder}']").First;
        await input.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await input.ClickAsync();
        await input.FillAsync(value);
        await input.PressAsync("Tab");
    }

    /// <summary>
    /// Fills a Syncfusion NumericTextBox located near a label with the given text.
    /// Finds the label, then locates the sibling numeric input in the same container.
    /// </summary>
    private async Task FillNumericByLabel(string labelText, string value)
    {
        // Find label, go to parent container, find the numeric input
        var label = Page.Locator($".card.bg-secondary label.form-label:text-is('{labelText}')").First;
        var container = label.Locator("xpath=..");
        var input = container.Locator("input.e-numerictextbox").First;

        await input.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await input.ClickAsync(new LocatorClickOptions { ClickCount = 3 });
        await input.FillAsync(value);
        await input.PressAsync("Tab");
    }

    /// <summary>
    /// Fills the Amount numeric input inside the reusable money row identified by its title.
    /// </summary>
    private async Task FillMoneyAmountByTitle(string rowTitle, string value)
    {
        var rowTitleLocator = Page.Locator($".manual-money-entry-title:text-is('{rowTitle}')").First;
        await rowTitleLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        var rowContainer = rowTitleLocator.Locator("xpath=..");
        var amountInput = rowContainer.Locator("input.e-numerictextbox").First;

        await amountInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await amountInput.ClickAsync(new LocatorClickOptions { ClickCount = 3 });
        await amountInput.FillAsync(value);
        await amountInput.PressAsync("Tab");
    }

    /// <summary>
    /// Fills a Syncfusion TextBox located near a label with the given text.
    /// Uses the e-textbox wrapper to disambiguate from numeric inputs.
    /// </summary>
    private async Task FillTextByLabel(string labelText, string value)
    {
        var label = Page.Locator($"label.form-label:has-text('{labelText}')").First;
        var container = label.Locator("xpath=..");
        var input = container.Locator("input.e-textbox").First;

        await input.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await input.ClickAsync();
        await input.FillAsync(value);
        await input.PressAsync("Tab");
    }

    /// <summary>
    /// Clicks a button by its visible text.
    /// </summary>
    private async Task ClickButtonByText(string buttonText)
    {
        var button = Page.Locator($"button.e-btn:has-text('{buttonText}')").First;
        await button.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await button.ScrollIntoViewIfNeededAsync();
        await button.ClickAsync();
    }

    /// <summary>
    /// Verifies a Syncfusion toast containing expected text appeared.
    /// </summary>
    private async Task VerifySuccessToast(string expectedText)
    {
        var toast = Page.Locator($".e-toast:has-text('{expectedText}')").First;
        await toast.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 10000
        });
        TestContext.WriteLine($"Success toast appeared containing '{expectedText}'");
    }

    /// <summary>
    /// Verifies the Added Entries grid has the expected number of data rows.
    /// </summary>
    private async Task VerifyGridRowCount(int expectedCount)
    {
        var rows = Page.Locator(".e-grid .e-row");
        await Expect(rows).ToHaveCountAsync(expectedCount, new LocatorAssertionsToHaveCountOptions { Timeout = 10000 });
        var count = await rows.CountAsync();
        TestContext.WriteLine($"Grid row count: {count}");
        Assert.That(count, Is.EqualTo(expectedCount),
            $"Expected {expectedCount} rows in the Added Entries grid, but found {count}");
    }

    /// <summary>
    /// Verifies the grid contains a cell with the expected text.
    /// </summary>
    private async Task VerifyGridContainsText(string expectedText)
    {
        var cell = Page.Locator($".e-grid .e-rowcell:has-text('{expectedText}')").First;
        await Expect(cell).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
    }
}
