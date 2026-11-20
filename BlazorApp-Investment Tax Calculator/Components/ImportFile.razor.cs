using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Parser;
using InvestmentTaxCalculator.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace InvestmentTaxCalculator.Components;

public partial class ImportFile : IDisposable
{
    [Inject] public required FileImportStateService FileImportState { get; set; }
    [Inject] public required ShareIdentityRegistry ShareIdentityRegistry { get; set; }

    private DuplicateWarningModal duplicateModal = default!;
    private CsvColumnMappingModal columnMappingModal = default!;

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
                    TaxEventLists events = await fileParseController.ReadFile(file, columnMappingModal.ShowAsync);
                    ShowDividendRegionUnknownWarning(events);
                    ShowStoredEriIncomeWarning(events);
                    // Build share identities over already imported events and the new file together, so a symbol
                    // pair split across statements (e.g. "DIS" in one and "DISm" in another) resolves to the same
                    // share, and duplicate signatures are compared using the canonical asset names.
                    ShareIdentityRegistry.RegisterEvents(taxEventLists.AllEvents.Concat(events.AllEvents));
                    ExecutionState executionState = await CheckDuplicateAndConfirm(events);
                    if (executionState is ExecutionState.SKIP_FILE) continue;
                    if (executionState is ExecutionState.SKIP_DUPLICATE) taxEventLists.AddData(events, true);
                    if (executionState is ExecutionState.INCLUDE_DUPLICATE) taxEventLists.AddData(events, false);
                    ShowDroppedEqualisationWarning(taxEventLists.RemoveUnlinkedEqualisations());
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

    /// <summary>
    /// Fund equalisation entries are only kept with the income they reduce. Tells the user about any left out.
    /// </summary>
    private void ShowDroppedEqualisationWarning(List<FundEqualisation> dropped)
    {
        if (dropped.Count == 0) return;
        toastService.ShowWarning($"{dropped.Count} fund equalisation entr{(dropped.Count == 1 ? "y was" : "ies were")} not imported because the income " +
            "reduced could not be found (not loaded, or the file was saved by a version before 1.8). Please enter again:\n" +
            string.Join("\n", dropped.Select(e => e.Reason)));
    }

    /// <summary>
    /// Versions before 1.8 stored the income of each ERI entry as a dividend or interest event. That income is now
    /// worked out from the ERI entry, so a file holding both would count it twice.
    /// </summary>
    private void ShowStoredEriIncomeWarning(TaxEventLists events)
    {
        IEnumerable<TaxEvent> storedEriIncome = events.Dividends.Where(d => d.DividendType == DividendType.EXCESS_REPORTABLE_INCOME).Cast<TaxEvent>()
            .Concat(events.InterestIncomes.Where(i => i.InterestType == InterestType.EXCESSREPORTABLEINCOME));
        List<ExcessReportableIncome> eris = [.. events.CorporateActions.OfType<ExcessReportableIncome>()];
        if (storedEriIncome.Any(income => eris.Any(eri => eri.AssetName == income.AssetName && eri.Date == income.Date)))
        {
            toastService.ShowWarning("This file holds ERI entries together with their income stored separately, as saved by versions before 1.8. " +
                "That ERI income will be counted twice. Sessions from those versions are not supported: please import the broker files again and re-enter ERI and equalisation.");
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
