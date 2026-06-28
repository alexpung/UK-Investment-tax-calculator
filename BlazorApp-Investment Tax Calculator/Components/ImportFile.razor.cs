using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Parser;
using InvestmentTaxCalculator.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

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

    private const int MaxImportFileCount = 1000;

    private async Task LoadFiles(InputFileChangeEventArgs args)
    {
        if (args.FileCount > MaxImportFileCount)
        {
            toastService.ShowError($"Too many files selected ({args.FileCount}). Please import at most {MaxImportFileCount} files at a time.");
            return;
        }

        var files = args.GetMultipleFiles(maximumFileCount: MaxImportFileCount);
        FileImportState.StartProcessing(files.Count);

        try
        {
            foreach (var file in files)
            {
                try
                {
                    TaxEventLists events = await fileParseController.ReadFile(file);
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
        }
    }

    private void ShowDividendRegionUnknownWarning(TaxEventLists events)
    {
        var dividendWithUnknownRegions = events.Dividends.Where(x => x.CompanyLocation == CountryCode.UnknownRegion);
        foreach (var dividend in dividendWithUnknownRegions)
        {
            // Plain text with line breaks; the toast renders Detail as escaped text, so the
            // asset name/description below are shown safely without manual HTML encoding.
            toastService.ShowWarning($"Unknown region detected with dividend data with:\n date: {dividend.Date.Date:d}\n" +
                $"company: {dividend.AssetName}\ndescription: {dividend.Proceed.Description}\n Please check the country for the company manually.");
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
