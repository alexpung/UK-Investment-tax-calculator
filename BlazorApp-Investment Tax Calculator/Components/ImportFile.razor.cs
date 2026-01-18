using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Parser;

using Syncfusion.Blazor.Inputs;

namespace InvestmentTaxCalculator.Components;

public partial class ImportFile
{
    private DuplicateWarningModal duplicateModal;

    private async Task LoadFiles(UploadChangeEventArgs args)
    {
        foreach (var file in args.Files)
        {
            try
            {
                TaxEventLists events = await fileParseController.ReadFile(file.File);
                ShowDividendRegionUnknownWarning(events);
                ExecutionState executionState = await CheckDuplicateAndConfirm(taxEventLists);
                if (executionState is ExecutionState.SKIP_FILE) continue;
                if (executionState is ExecutionState.ABORT) break;
                if (executionState is ExecutionState.SKIP_DUPLICATE) taxEventLists.AddData(events, true);
                if (executionState is ExecutionState.INCLUDE_DUPLICATE) taxEventLists.AddData(events, false);
                OptionHelper.CheckOptions(taxEventLists);
            }
            catch (Exception ex)
            {
                toastService.ShowException(ex);
            }
        }
        args.Files.Clear();
    }

    private void ShowDividendRegionUnknownWarning(TaxEventLists events)
    {
        var dividendWithUnknownRegions = events.Dividends.Where(x => x.CompanyLocation == CountryCode.UnknownRegion);
        foreach (var dividend in dividendWithUnknownRegions)
        {
            toastService.ShowWarning($"Unknown region detected with dividend data with:<br> date: {dividend.Date.Date.ToShortDateString()}<br>" +
                $"company: {dividend.AssetName}<br>description: {dividend.Proceed.Description}<br> Please check the country for the company manually.");
        }
    }

    private async Task<ExecutionState> CheckDuplicateAndConfirm(TaxEventLists events)
    {
        var duplicates = taxEventLists.GetDuplicates(events);
        int duplicateCount = duplicates.GetTotalNumberOfEvents();

        if (duplicateCount > 10)
        {
            toastService.ShowError($"Import rejected. Found {duplicateCount} duplicates, which exceeds the limit of 10.");
            return ExecutionState.SKIP_FILE;
        }
        if (duplicateCount > 0)
        {
            bool? skipDuplicates = await duplicateModal.ShowAsync(duplicates);
            if (skipDuplicates is null) return ExecutionState.ABORT;
            if ((bool)skipDuplicates) return ExecutionState.SKIP_DUPLICATE;
        }
        return ExecutionState.INCLUDE_DUPLICATE;
    }

    private enum ExecutionState
    {
        SKIP_FILE,
        ABORT,
        INCLUDE_DUPLICATE,
        SKIP_DUPLICATE
    }
}