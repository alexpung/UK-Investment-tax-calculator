using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

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
                CheckOptions();
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

    private void CheckOptions()
                {
        List<OptionTrade> tradesToCheck = [.. taxEventLists.OptionTrades.Where(trade => trade is OptionTrade
        { TradeReason: TradeReason.OwnerExerciseOption or TradeReason.OptionAssigned, SettlementMethod: SettlementMethods.UNKNOWN  })];
        foreach (var optionTrade in tradesToCheck)
        {
            bool isSettled = CheckIfOptionIsDeliverySettled(optionTrade) || CheckIfOptionIsCashSettled(optionTrade);
            if (!isSettled)
            {
                throw new InvalidOperationException($"No corresponding {optionTrade.TradeReason} trade found for option (Underlying: {optionTrade.Underlying}, " +
                $"Quantity: {optionTrade.Quantity * optionTrade.Multiplier}, date: {optionTrade.Date.Date}, there is likely an omission of trade(s) in the input)");
                }
        }
    }
                
    private bool CheckIfOptionIsDeliverySettled(OptionTrade optionTrade)
                {
        var underlyingTrade = taxEventLists.Trades.Find(trade =>
                                                        trade.AssetName == optionTrade.Underlying &&
                                                        trade.TradeReason == optionTrade.TradeReason &&
                                                        Math.Abs(trade.Quantity) == Math.Abs(optionTrade.Quantity * optionTrade.Multiplier) &&
                                                        trade.Date.Date == optionTrade.Date.Date);
        if (underlyingTrade is not null)
                    {
            optionTrade.ExerciseOrExercisedTrade = underlyingTrade;
            optionTrade.SettlementMethod = SettlementMethods.DELIVERY;
            return true;
                    }
        return false;
                }

    private bool CheckIfOptionIsCashSettled(OptionTrade optionTrade)
                {
        var matchingCashSettlement = taxEventLists.CashSettlements.Find(trade => trade.AssetName == optionTrade.AssetName &&
                                                                                     trade.Date.Date == optionTrade.Date.Date &&
                                                                                     trade.TradeReason == optionTrade.TradeReason);
        if (matchingCashSettlement is not null)
        {
            optionTrade.SettlementMethod = SettlementMethods.CASH;
            WrappedMoney tradeValue;
            if (matchingCashSettlement.TradeReason == TradeReason.OptionAssigned) tradeValue = matchingCashSettlement.Amount * -1;
            else tradeValue = matchingCashSettlement.Amount;
            optionTrade.GrossProceed = optionTrade.GrossProceed with { Amount = tradeValue, Description = matchingCashSettlement.Description };
            return true;
                }
        return false;
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