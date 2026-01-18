using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Parser;

public static class OptionHelper
{
    public static void CheckOptions(TaxEventLists taxEventLists)
    {
        List<OptionTrade> tradesToCheck = [.. taxEventLists.OptionTrades.Where(trade => trade is OptionTrade
        { TradeReason: TradeReason.OwnerExerciseOption or TradeReason.OptionAssigned, SettlementMethod: SettlementMethods.UNKNOWN  })];
        foreach (var optionTrade in tradesToCheck)
        {
            bool isSettled = CheckIfOptionIsDeliverySettled(optionTrade, taxEventLists) || CheckIfOptionIsCashSettled(optionTrade, taxEventLists);
            if (!isSettled)
            {
                throw new InvalidOperationException($"No corresponding {optionTrade.TradeReason} trade found for option (Underlying: {optionTrade.Underlying}, " +
                $"Quantity: {optionTrade.Quantity * optionTrade.Multiplier}, date: {optionTrade.Date.Date}, there is likely an omission of trade(s) in the input)");
            }
        }
    }

    private static bool CheckIfOptionIsDeliverySettled(OptionTrade optionTrade, TaxEventLists taxEventLists)
    {
        var underlyingTrade = taxEventLists.Trades.Find(trade =>
                                                        trade.AssetName == optionTrade.Underlying &&
                                                        trade.TradeReason == optionTrade.TradeReason &&
                                                        Math.Abs(trade.Quantity) == Math.Abs(optionTrade.Quantity * optionTrade.Multiplier) &&
                                                        trade.Date.Date == optionTrade.Date.Date);
        if (underlyingTrade is not null)
        {
            optionTrade.ExerciseOrExercisedTrade = underlyingTrade;
            foreach (var item in taxEventLists.OptionTrades.Where(trade => trade.AssetName == optionTrade.AssetName))
            {
                item.SettlementMethod = SettlementMethods.DELIVERY;
            }
            return true;
        }
        return false;
    }

    private static bool CheckIfOptionIsCashSettled(OptionTrade optionTrade, TaxEventLists taxEventLists)
    {
        var matchingCashSettlement = taxEventLists.CashSettlements.Find(trade => trade.AssetName == optionTrade.AssetName &&
                                                                                     trade.Date.Date == optionTrade.Date.Date &&
                                                                                     trade.TradeReason == optionTrade.TradeReason);
        if (matchingCashSettlement is not null)
        {
            foreach (var item in taxEventLists.OptionTrades.Where(i => i.AssetName == optionTrade.AssetName))
            {
                item.SettlementMethod = SettlementMethods.CASH;
            }
            WrappedMoney tradeValue;
            if (matchingCashSettlement.TradeReason == TradeReason.OptionAssigned) tradeValue = matchingCashSettlement.Amount * -1;
            else tradeValue = matchingCashSettlement.Amount;
            optionTrade.GrossProceed = optionTrade.GrossProceed with { Amount = tradeValue, Description = matchingCashSettlement.Description, FxRate = 1 };
            return true;
        }
        return false;
    }
}
