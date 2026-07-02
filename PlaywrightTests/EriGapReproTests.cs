using Microsoft.Playwright;
using NUnit.Framework;

namespace PlaywrightTests;

/// <summary>
/// Reproduces the reported ERI gap-period issue end-to-end:
/// 1. Add a manual stock acquisition (100 units, £1000, 01/01/2025)
/// 2. Add a manual stock disposal (50 units, £600, 01/08/2025)
/// 3. Add an ERI on the ERI &amp; Equalisation page (period end 01/06/2025, 0.2/share)
/// 4. Go to Import &amp; Export and press Start Calculation several times
/// 5. Export trades text and the data JSON
/// Expected: disposal allowable cost £510 (uplift £10 under reg. 99(5)), pool +£10.
/// </summary>
[TestFixture]
public class EriGapReproTests : PlaywrightTestBase
{
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();
        options.Locale = "en-GB";
        options.AcceptDownloads = true;
        return options;
    }

    [Test]
    public async Task ManualEntriesThenEriThenCalculate_DisposalShouldBeUplifted()
    {
        var consoleErrors = new List<string>();
        Page.Console += (_, msg) => { if (msg.Type == "error") consoleErrors.Add(msg.Text); };

        // ── Step 1: manual acquisition (single full load; all later navigation is SPA so state persists) ──
        await NavigateAndWaitForBlazorAsync("/AddTradePage");
        await WaitForTextAsync("Add Stock Trade");
        await FillDateByLabel("Date", "01/01/2025");
        await FillComboBox("Enter ticker or asset name", "Test fund");
        await FillNumericByLabel("Quantity", "100");
        await FillMoneyAmountByTitle("Gross Proceed", "1000");
        await ClickButtonByText("Add Stock Trade");
        await WaitForToast("added");

        // ── Step 2: manual disposal ──
        await FillDateByLabel("Date", "01/08/2025");
        await FillComboBox("Enter ticker or asset name", "Test fund");
        await SelectDropDownByLabel("Trade Type", "Disposal");
        await FillNumericByLabel("Quantity", "50");
        await FillMoneyAmountByTitle("Gross Proceed", "600");
        await ClickButtonByText("Add Stock Trade");
        await WaitForToast("added");

        // ── Step 3: add ERI (SPA navigation triggers a recalculation first) ──
        await NavigateViaSidebar("Configurations and manual inputs", "ERI and Equalisation");
        await FillDateByPlaceholder("Pick period end date", "01/06/2025");
        await Page.ScreenshotAsync(new() { Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "eri-page.png"), FullPage = true });
        await SelectTickerDropdown("Test fund");
        await FillNumericByLabel("Income Per Share (Local Currency)", "0.2");
        await ClickButtonByText("Add ERI Entry");
        await WaitForToast("Successfully added ERI");

        // ── Step 4: go to import page (SPA), press Start Calculation a few times ──
        await NavigateViaSidebar("Import + PDF", "Import/Export");
        for (int i = 0; i < 3; i++)
        {
            await ClickButtonByText("Start Calculation");
            await WaitForToast("Calculation completed");
            await DismissToasts();
        }

        // ── Step 5: export trades text + data json ──
        string tradesText = await DownloadFromButton("Export Trades");
        string dataJson = await DownloadFromButton("Export data for future import");

        TestContext.WriteLine("==== TRADES EXPORT ====");
        TestContext.WriteLine(tradesText);
        TestContext.WriteLine("==== ERI JSON ====");
        int eriIdx = dataJson.IndexOf("\"$type\": \"eri\"", StringComparison.Ordinal);
        TestContext.WriteLine(eriIdx >= 0 ? dataJson.Substring(eriIdx, Math.Min(1200, dataJson.Length - eriIdx)) : "(no eri found)");
        TestContext.WriteLine("==== CONSOLE ERRORS ====");
        consoleErrors.ForEach(TestContext.WriteLine);

        // ── Step 6: assert the disposal got the reg. 99(5) uplift ──
        Assert.That(tradesText, Does.Contain("510"), "Disposal allowable cost should be uplifted to £510");
        Assert.That(tradesText, Does.Contain("reg. 99(5)"), "Disposal should carry the reg. 99(5) explanation");
    }

    // ───────────────────── helpers ─────────────────────

    private async Task NavigateViaSidebar(string categoryTitle, string linkText)
    {
        await ExpandNavCategoryAsync(categoryTitle);
        var link = Page.Locator($".sidebar-nav a:has-text('{linkText}')").First;
        await link.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await link.ClickAsync();
        await Task.Delay(1500); // allow the navigation-triggered recalculation and render to finish
    }

    private async Task FillDateByLabel(string labelText, string value)
    {
        var label = Page.Locator($"label.form-label:text-is('{labelText}')").First;
        var container = label.Locator("xpath=..");
        var input = container.Locator("input").First;
        await input.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await input.ClickAsync(new LocatorClickOptions { ClickCount = 3 });
        await input.FillAsync(value);
        await input.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Escape"); // close any calendar popup
    }

    private async Task FillDateByPlaceholder(string placeholder, string value)
    {
        var input = Page.Locator($"input[placeholder='{placeholder}']").First;
        await input.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await input.ClickAsync(new LocatorClickOptions { ClickCount = 3 });
        await input.FillAsync(value);
        await input.PressAsync("Tab"); // blur commits the Radzen date picker change event
        await Task.Delay(500);
        await Page.Keyboard.PressAsync("Escape");
        await Task.Delay(300);
    }

    private async Task FillComboBox(string placeholder, string value)
    {
        var input = Page.Locator($"input[placeholder='{placeholder}']").First;
        await input.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await input.ClickAsync(new LocatorClickOptions { ClickCount = 3 });
        await input.FillAsync(value);
        await input.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Escape");
    }

    private async Task FillNumericByLabel(string labelText, string value)
    {
        var label = Page.Locator($"label.form-label:text-is('{labelText}')").First;
        var container = label.Locator("xpath=..");
        var input = container.Locator("input").First;
        await input.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await input.ClickAsync(new LocatorClickOptions { ClickCount = 3 });
        await input.FillAsync(value);
        await input.PressAsync("Tab");
    }

    private async Task FillMoneyAmountByTitle(string rowTitle, string value)
    {
        var rowTitleLocator = Page.Locator($".manual-money-entry-title:text-is('{rowTitle}')").First;
        await rowTitleLocator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
        var rowContainer = rowTitleLocator.Locator("xpath=..");
        var amountInput = rowContainer.Locator("input").First;
        await amountInput.ClickAsync(new LocatorClickOptions { ClickCount = 3 });
        await amountInput.FillAsync(value);
        await amountInput.PressAsync("Tab");
    }

    private async Task SelectDropDownByLabel(string labelText, string itemText)
    {
        var label = Page.Locator($"label.form-label:text-is('{labelText}')").First;
        var container = label.Locator("xpath=..");
        var dropdown = container.Locator(".rz-dropdown").First;
        await dropdown.ClickAsync();
        var item = Page.Locator($".rz-dropdown-item:has-text('{itemText}')").First;
        await item.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await item.ClickAsync();
    }

    private async Task SelectTickerDropdown(string ticker)
    {
        // The ERI ticker selector is the first 'Ticker'-labelled RadzenDropDown on the page
        var label = Page.Locator("label.form-label:text-is('Ticker')").First;
        var container = label.Locator("xpath=..");
        var dropdown = container.Locator(".rz-dropdown").First;
        await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        var item = Page.Locator($".rz-dropdown-item:has-text('{ticker}')").First;
        for (int attempt = 0; attempt < 5 && !await item.IsVisibleAsync(); attempt++)
        {
            switch (attempt % 3)
            {
                case 0: await dropdown.ClickAsync(); break;
                case 1: await dropdown.FocusAsync(); await Page.Keyboard.PressAsync("Enter"); break;
                case 2: await dropdown.FocusAsync(); await Page.Keyboard.PressAsync("Alt+ArrowDown"); break;
            }
            await Task.Delay(700);
        }
        if (await item.IsVisibleAsync())
        {
            await item.ClickAsync();
        }
        else
        {
            // last resort: keyboard-select the first (only) option
            await dropdown.FocusAsync();
            await Page.Keyboard.PressAsync("ArrowDown");
            await Task.Delay(300);
            await Page.Keyboard.PressAsync("Enter");
        }
        await Task.Delay(500);
    }

    private async Task ClickButtonByText(string buttonText)
    {
        var button = Page.Locator($"button:has-text('{buttonText}')").First;
        await button.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        await button.ScrollIntoViewIfNeededAsync();
        await button.ClickAsync();
    }

    private async Task WaitForToast(string expectedText)
    {
        var toast = Page.Locator($".rz-notification:has-text('{expectedText}')").First;
        await toast.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
    }

    private async Task DismissToasts()
    {
        // let notifications fade so subsequent WaitForToast sees fresh ones
        await Task.Delay(4500);
    }

    private async Task<string> DownloadFromButton(string buttonText)
    {
        var download = await Page.RunAndWaitForDownloadAsync(async () =>
        {
            await ClickButtonByText(buttonText);
        }, new PageRunAndWaitForDownloadOptions { Timeout = 30000 });
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, download.SuggestedFilename);
        await download.SaveAsAsync(path);
        return await File.ReadAllTextAsync(path);
    }
}
