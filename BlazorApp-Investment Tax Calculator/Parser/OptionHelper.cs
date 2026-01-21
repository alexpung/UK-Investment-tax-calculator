using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Parser;

public static class OptionHelper
{
    public static void CheckOptions(TaxEventLists taxEventLists)
    {
        // 0. Validate multiplier consistency across all trades for the same option
        var multiplierGroups = taxEventLists.OptionTrades
            .GroupBy(o => o.AssetName)
            .Where(g => g.Select(o => o.Multiplier).Distinct().Count() > 1);

        if (multiplierGroups.Any())
        {
            string failedTickers = string.Join(", ", multiplierGroups.Select(g => g.Key));
            throw new InvalidOperationException($"Inconsistent multipliers found for the same option asset {failedTickers}");
        }

        List<OptionTrade> tradesToCheck = [.. taxEventLists.OptionTrades.Where(trade => trade is OptionTrade
        { TradeReason: TradeReason.OwnerExerciseOption or TradeReason.OptionAssigned, SettlementMethod: SettlementMethods.UNKNOWN  })];

        // Create candidates pools
        var availableTrades = new List<Trade>(taxEventLists.Trades);
        var availableCashSettlements = new List<CashSettlement>(taxEventLists.CashSettlements);

        foreach (var optionTrade in tradesToCheck)
        {
            bool isSettled = CheckIfOptionIsDeliverySettled(optionTrade, availableTrades, taxEventLists.OptionTrades) ||
                             CheckIfOptionIsCashSettled(optionTrade, availableCashSettlements, taxEventLists.OptionTrades);
            if (!isSettled)
            {
                throw new InvalidOperationException($"No corresponding {optionTrade.TradeReason} trade found for option (Underlying: {optionTrade.Underlying}, " +
                $"Quantity: {optionTrade.Quantity * optionTrade.Multiplier}, date: {optionTrade.Date.Date}, there is likely an omission of trade(s) in the input)");
            }
        }
    }

    private static bool CheckIfOptionIsDeliverySettled(OptionTrade optionTrade, List<Trade> availableTrades, List<OptionTrade> allOptions)
    {
        var underlyingTrade = availableTrades.Find(trade =>
                                                        trade.AssetName == optionTrade.Underlying &&
                                                        trade.TradeReason == optionTrade.TradeReason &&
                                                        Math.Abs(trade.Quantity) == Math.Abs(optionTrade.Quantity * optionTrade.Multiplier) &&
                                                        trade.Date.Date == optionTrade.Date.Date);
        if (underlyingTrade is not null)
        {
            availableTrades.Remove(underlyingTrade); // Consume the trade
            optionTrade.ExerciseOrExercisedTrade = underlyingTrade;

            foreach (var item in allOptions.Where(trade => trade.AssetName == optionTrade.AssetName))
            {
                item.SettlementMethod = SettlementMethods.DELIVERY;
            }

            return true;
        }
        return false;
    }

    private static bool CheckIfOptionIsCashSettled(OptionTrade optionTrade, List<CashSettlement> availableSettlements, List<OptionTrade> allOptions)
    {
        var matchingCashSettlement = availableSettlements.Find(trade => trade.AssetName == optionTrade.AssetName &&
                                                                                     trade.Date.Date == optionTrade.Date.Date &&
                                                                                     trade.TradeReason == optionTrade.TradeReason);
        if (matchingCashSettlement is not null)
        {
            availableSettlements.Remove(matchingCashSettlement); // Consume the settlement

            foreach (var item in allOptions.Where(i => i.AssetName == optionTrade.AssetName))
            {
                item.SettlementMethod = SettlementMethods.CASH;
            }

            DescribedMoney tradeValue;
            if (matchingCashSettlement.TradeReason == TradeReason.OptionAssigned) tradeValue = matchingCashSettlement.Amount with { Amount = -matchingCashSettlement.Amount.Amount };
            else tradeValue = matchingCashSettlement.Amount;
            optionTrade.GrossProceed = tradeValue;
            return true;
        }
        return false;
    }
}
