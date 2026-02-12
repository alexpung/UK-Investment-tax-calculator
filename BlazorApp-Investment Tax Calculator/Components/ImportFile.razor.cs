using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Parser;
using InvestmentTaxCalculator.Services;

using Microsoft.AspNetCore.Components;

using Syncfusion.Blazor.Inputs;

namespace InvestmentTaxCalculator.Components;

public partial class ImportFile : IDisposable
{
    [Inject] public required FileImportStateService FileImportState { get; set; }

    private DuplicateWarningModal duplicateModal = default!;

    protected override void OnInitialized()
    {
        FileImportState.OnChange += StateHasChanged;
    }

    public void Dispose()
    {
        FileImportState.OnChange -= StateHasChanged;
    }

    private async Task LoadFiles(UploadChangeEventArgs args)
    {
        FileImportState.StartProcessing(args.Files.Count);

        try
        {
            foreach (var file in args.Files)
            {
                try
                {
                    TaxEventLists events = await fileParseController.ReadFile(file.File);
                    ShowDividendRegionUnknownWarning(events);
                    ExecutionState executionState = await CheckDuplicateAndConfirm(events);
                    if (executionState is ExecutionState.SKIP_FILE) continue;
                    if (executionState is ExecutionState.SKIP_DUPLICATE) taxEventLists.AddData(events, true);
                    if (executionState is ExecutionState.INCLUDE_DUPLICATE) taxEventLists.AddData(events, false);
                    OptionHelper.CheckOptions(taxEventLists);
                }
                catch (Exception ex)
                {
                    toastService.ShowException(ex);
                }
                finally
                {
                    FileImportState.IncrementProcessedFiles();
                }
            }
        }
        finally
        {
            FileImportState.CompleteProcessing();
            args.Files.Clear();
        }
    }

    private void ShowDividendRegionUnknownWarning(TaxEventLists events)
    {
        var dividendWithUnknownRegions = events.Dividends.Where(x => x.CompanyLocation == CountryCode.UnknownRegion);
        foreach (var dividend in dividendWithUnknownRegions)
        {
            string encodedAssetName = System.Net.WebUtility.HtmlEncode(dividend.AssetName);
            string encodedDescription = System.Net.WebUtility.HtmlEncode(dividend.Proceed.Description);

            toastService.ShowWarning($"Unknown region detected with dividend data with:<br> date: {dividend.Date.Date:d}<br>" +
                $"company: {encodedAssetName}<br>description: {encodedDescription}<br> Please check the country for the company manually.");
        }
    }

    private async Task<ExecutionState> CheckDuplicateAndConfirm(TaxEventLists newEvents)
    {
        var duplicates = taxEventLists.GetDuplicates(newEvents);
        int duplicateCount = duplicates.GetTotalNumberOfEvents();

        if (duplicateCount > 10)
        {
            toastService.ShowError($"Import rejected. Found {duplicateCount} duplicates, which exceeds the limit of 10.");
            return ExecutionState.SKIP_FILE;
        }
        if (duplicateCount > 0)
        {
            bool skipDuplicates = await duplicateModal.ShowAsync(duplicates);
            if (skipDuplicates) return ExecutionState.SKIP_DUPLICATE;
        }
        return ExecutionState.INCLUDE_DUPLICATE;
    }

    private enum ExecutionState
    {
        SKIP_FILE,
        INCLUDE_DUPLICATE,
        SKIP_DUPLICATE
    }
}
