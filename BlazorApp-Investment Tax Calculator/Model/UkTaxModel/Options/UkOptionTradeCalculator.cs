using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;
using InvestmentTaxCalculator.Parser;
using InvestmentTaxCalculator.Services;

namespace InvestmentTaxCalculator.Model.UkTaxModel.Options;

public class UkOptionTradeCalculator(UkSection104Pools section104Pools, ITradeAndCorporateActionList tradeList, ITaxYear taxYear, ToastService toastService) : ITradeCalculator
{
    public List<ITradeTaxCalculation> CalculateTax()
    {
        MatchExeciseAndAssignmentOptionTrade();
        List<OptionTradeTaxCalculation> tradeTaxCalculations = [.. GroupTrade(tradeList.OptionTrades)];
        GroupedTradeContainer<OptionTradeTaxCalculation> _tradeContainer = new(tradeTaxCalculations, tradeList.CorporateActions);
        foreach (var match in UkMatchingRules.ApplySameDayMatchingRule(_tradeContainer))
        {
            MatchTrade(match.Item1, match.Item2, TaxMatchType.SAME_DAY);
        }
        foreach (var match in UkMatchingRules.ApplyBedAndBreakfastMatchingRule(_tradeContainer))
        {
            MatchTrade(match.Item1, match.Item2, TaxMatchType.BED_AND_BREAKFAST);
        }
        foreach (var match in UkMatchingRules.ProcessTradeInChronologicalOrder(section104Pools, _tradeContainer))
        {
            MatchTrade(match.Item1, match.Item2, TaxMatchType.SHORTCOVER);
        }
        return tradeTaxCalculations.Cast<ITradeTaxCalculation>().ToList();
    }

    private void MatchExeciseAndAssignmentOptionTrade()
    {
        List<OptionTrade> filteredTrades = tradeList.OptionTrades.Where(trade => trade is OptionTrade
        { TradeReason: TradeReason.OwnerExerciseOption or TradeReason.OptionAssigned }).ToList();
        foreach (var optionTrade in filteredTrades)
        {
            var underlyingTrade = tradeList.Trades.Find(trade =>
                                                        trade.AssetName == optionTrade.Underlying &&
                                                        trade.TradeReason == optionTrade.TradeReason &&
                                                        Math.Abs(trade.Quantity) == Math.Abs(optionTrade.Quantity * optionTrade.Multiplier) &&
                                                        trade.Date.Date == optionTrade.Date.Date);
            if (underlyingTrade is not null)
            {
                optionTrade.ExerciseOrExercisedTrade = underlyingTrade;
            }
            else
            {
                var matchingCashSettlement = tradeList.CashSettlements.Find(trade => trade.AssetName == optionTrade.AssetName &&
                                                                                     trade.Date.Date == optionTrade.Date.Date &&
                                                                                     trade.TradeReason == optionTrade.TradeReason);
                if (matchingCashSettlement is not null)
                {
                    optionTrade.CashSettled = true;
                    WrappedMoney tradeValue;
                    if (matchingCashSettlement.TradeReason == TradeReason.OptionAssigned) tradeValue = matchingCashSettlement.Amount * -1;
                    else tradeValue = matchingCashSettlement.Amount;
                    optionTrade.GrossProceed = optionTrade.GrossProceed with { Amount = tradeValue, Description = matchingCashSettlement.Description };
                }
                else
                {
                    toastService.ShowError($"No corresponding {optionTrade.TradeReason} trade found for option (Underlying: {optionTrade.Underlying}, " +
                    $"Quantity: {optionTrade.Quantity * optionTrade.Multiplier}, date: {optionTrade.Date.Date}, there is likely an omission of trade(s) in the input)");
                }
            }
        }
    }

    private static List<OptionTradeTaxCalculation> GroupTrade(IEnumerable<OptionTrade> trades)
    {
        var groupedTrade = from trade in trades
                           group trade by new { trade.AssetName, trade.Date.Date, trade.AcquisitionDisposal };
        return groupedTrade.Select(group => new OptionTradeTaxCalculation(group)).ToList();
    }

    public void MatchTrade(OptionTradeTaxCalculation trade1, OptionTradeTaxCalculation trade2, TaxMatchType taxMatchType)
    {
        TradePairSorter<OptionTradeTaxCalculation> tradePairSorter = new(trade1, trade2);
        if (trade1.CalculationCompleted || trade2.CalculationCompleted) return;
        if (trade1.UnmatchedQty == 0 || trade2.UnmatchedQty == 0) return;

        // This part of the algo handle the case when you trade an option at the date of expiry and you traded/it expires/it is assigned/you execise it in the same day
        // 
        decimal matchRatio;
        if (tradePairSorter.LatterTrade.UnmatchedQty < tradePairSorter.EarlierTrade.UnmatchedQty)
        {
            // (LatterTrade.UnmatchedQty/EarlierTrade.UnmatchedQty) * (LatterTrade.UnmatchedQty/LatterTrade.TotalQty)
            matchRatio = tradePairSorter.LatterTrade.UnmatchedQty * tradePairSorter.LatterTrade.UnmatchedQty / (tradePairSorter.EarlierTrade.UnmatchedQty * tradePairSorter.LatterTrade.TotalQty);
        }
        else if (tradePairSorter.LatterTrade.UnmatchedQty > tradePairSorter.EarlierTrade.UnmatchedQty)
        {
            matchRatio = tradePairSorter.EarlierTrade.UnmatchedQty * tradePairSorter.LatterTrade.UnmatchedQty / (tradePairSorter.LatterTrade.UnmatchedQty * tradePairSorter.LatterTrade.TotalQty);
        }
        else
        {
            matchRatio = tradePairSorter.LatterTrade.UnmatchedQty / tradePairSorter.LatterTrade.TotalQty;
        }
        decimal assignmentQty = tradePairSorter.LatterTrade.AssignedQty * matchRatio;
        decimal expiredQty = tradePairSorter.LatterTrade.ExpiredQty * matchRatio;
        decimal exercisedQty = tradePairSorter.LatterTrade.OwnerExercisedQty * matchRatio;
        if (assignmentQty > 0 && exercisedQty > 0) throw new ParseException($"{tradePairSorter.LatterTrade} has both assignment and exercise which is impossible");
        if (expiredQty > 0) MatchExpiredOption(tradePairSorter, taxMatchType, expiredQty);
        if (exercisedQty > 0) MatchExercisedOption(tradePairSorter, taxMatchType, exercisedQty);
        if (assignmentQty > 0) MatchAssignedOption(tradePairSorter, taxMatchType, assignmentQty);
        MatchNormalTrade(tradePairSorter, taxMatchType);
    }

    private void MatchNormalTrade(TradePairSorter<OptionTradeTaxCalculation> tradePairSorter, TaxMatchType taxMatchType)
    {
        WrappedMoney allowableCost = tradePairSorter.AcquisitionTrade.GetProportionedCostOrProceedForTradeReason(TradeReason.OrderedTrade, tradePairSorter.AcquisitionMatchQuantity);
        WrappedMoney disposalProceed = tradePairSorter.DisposalTrade.GetProportionedCostOrProceedForTradeReason(TradeReason.OrderedTrade, tradePairSorter.DisposalMatchQuantity);
        // If written an option and the position is held to next tax year, full disposal proceed is taxed and allowable cost can only be reclaimed when the trade is closed.
        if (tradePairSorter.EarlierTrade.AcquisitionDisposal == TradeType.DISPOSAL && taxYear.ToTaxYear(tradePairSorter.EarlierTrade.Date) != taxYear.ToTaxYear(tradePairSorter.LatterTrade.Date))
        {
            TaxRepay refundPremium = new(
                TaxYear: taxYear.ToTaxYear(tradePairSorter.LatterTrade.Date),
                RefundAmount: allowableCost,
                Reason: $"Sold option with ID:{tradePairSorter.EarlierTrade.Id} and position closed in later tax year {taxYear.ToTaxYear(tradePairSorter.LatterTrade.Date)}"
                );
            tradePairSorter.EarlierTrade.TaxRepayList.Add(refundPremium);
            allowableCost = WrappedMoney.GetBaseCurrencyZero();
        }
        TradeMatch disposalTradeMatch = CreateTradeMatch(tradePairSorter, tradePairSorter.AcquisitionMatchQuantity,
            allowableCost, disposalProceed, string.Empty, taxMatchType);
        TradeMatch AcquisitionTradeMatch = disposalTradeMatch with
        {
            BaseCurrencyMatchAllowableCost = WrappedMoney.GetBaseCurrencyZero(),
            BaseCurrencyMatchDisposalProceed = WrappedMoney.GetBaseCurrencyZero(),
        };
        AssignTradeMatch(tradePairSorter, tradePairSorter.AcquisitionMatchQuantity, AcquisitionTradeMatch, disposalTradeMatch);
    }

    /// <summary>
    /// An option expires.
    /// If you are the writer no allowable cost can be deducted as you earn the full premium.
    /// If you are the buyer full premium is counted as allowable cost.
    /// </summary>
    private static void MatchExpiredOption(TradePairSorter<OptionTradeTaxCalculation> tradePairSorter, TaxMatchType taxMatchType, decimal expiredQty)
    {
        TradeMatch disposalTradeMatch;
        // You sold an option and it expires
        if (tradePairSorter.EarlierTrade.AcquisitionDisposal == TradeType.DISPOSAL)
        {
            disposalTradeMatch = CreateTradeMatch(tradePairSorter, expiredQty, WrappedMoney.GetBaseCurrencyZero(),
            tradePairSorter.DisposalTrade.GetProportionedCostOrProceed(expiredQty), "The granted option expired. No allowable cost is added.", taxMatchType);
        }
        // You bought an option and it expires
        else
        {
            disposalTradeMatch = CreateTradeMatch(tradePairSorter, expiredQty, tradePairSorter.AcquisitionTrade.GetProportionedCostOrProceed(expiredQty),
            tradePairSorter.DisposalTrade.GetProportionedCostOrProceedForTradeReason(TradeReason.Expired, expiredQty), "The bought option expired. Full premium is added to allowable cost.", taxMatchType);
        }
        TradeMatch acquisitionTradeMatch = disposalTradeMatch with
        {
            BaseCurrencyMatchAllowableCost = WrappedMoney.GetBaseCurrencyZero(),
            BaseCurrencyMatchDisposalProceed = WrappedMoney.GetBaseCurrencyZero(),
        };
        AssignTradeMatch(tradePairSorter, expiredQty, acquisitionTradeMatch, disposalTradeMatch);
    }

    /// <summary>
    /// You bought an option and you exercise it.
    /// The option trade get rolled up to the exercised acquisition and disposal of the underlying and the option trade have no tax effect.
    /// Call option: allowable cost = allowable cost for buying the underlying + premium paid
    /// Put option: sale proceed = sale proceed for the underlying - premium paid
    /// </summary>
    private static void MatchExercisedOption(TradePairSorter<OptionTradeTaxCalculation> tradePairSorter, TaxMatchType taxMatchType, decimal exercisedQty)
    {
        TradeMatch tradeMatch;
        if (tradePairSorter.LatterTrade.IsCashSettled)
        {
            WrappedMoney allowableCost = tradePairSorter.AcquisitionTrade.GetProportionedCostOrProceedForTradeReason(TradeReason.OrderedTrade, tradePairSorter.AcquisitionMatchQuantity);
            WrappedMoney disposalProceed = tradePairSorter.DisposalTrade.GetProportionedCostOrProceedForTradeReason(TradeReason.OwnerExerciseOption, tradePairSorter.DisposalMatchQuantity);
            tradeMatch = CreateTradeMatch(tradePairSorter, exercisedQty, allowableCost, disposalProceed, $"{exercisedQty:F2} option cash settled.", taxMatchType);
        }
        else
        {
            WrappedMoney premiumCost = tradePairSorter.EarlierTrade.GetProportionedCostOrProceed(exercisedQty);
            // If there is mutiple exercise trades it doesn't matter which trade to roll up, as all trades are the same ticker and same day are treated as a sigle trade.
            tradePairSorter.LatterTrade.AttachTradeToUnderlying(premiumCost,
                $"Trade is created by option exercise of option with premium {premiumCost} added(subtracted) on {tradePairSorter.LatterTrade.Date:d}",
                TradeReason.OwnerExerciseOption);
            tradeMatch = CreateTradeMatch(tradePairSorter, exercisedQty, WrappedMoney.GetBaseCurrencyZero(), WrappedMoney.GetBaseCurrencyZero(),
                $"{exercisedQty} option exercised.", taxMatchType);
        }
        AssignTradeMatch(tradePairSorter, exercisedQty, tradeMatch, tradeMatch);
    }

    /// <summary>
    /// You sold an option and get an assignment.
    /// The option trade get rolled up to the assignment acquisition and disposal of the underlying and the option trade have no tax effect.
    /// Call option: sale proceed = sale proceed for the underlying + premium already received
    /// Put option: allowable cost = allowable cost for buying the underlying - premium already received
    /// </summary>
    private void MatchAssignedOption(TradePairSorter<OptionTradeTaxCalculation> tradePairSorter, TaxMatchType taxMatchType, decimal assignmentQty)
    {
        TradeMatch tradeMatch;
        if (tradePairSorter.LatterTrade.IsCashSettled)
        {
            WrappedMoney allowableCost = tradePairSorter.AcquisitionTrade.GetProportionedCostOrProceedForTradeReason(TradeReason.OptionAssigned, tradePairSorter.AcquisitionMatchQuantity);
            WrappedMoney disposalProceed = tradePairSorter.DisposalTrade.GetProportionedCostOrProceedForTradeReason(TradeReason.OrderedTrade, tradePairSorter.DisposalMatchQuantity);
            tradeMatch = CreateTradeMatch(tradePairSorter, assignmentQty, allowableCost, disposalProceed, $"{assignmentQty:F2} option cash settled.", taxMatchType);
        }
        else
        {
            WrappedMoney premiumCost = tradePairSorter.EarlierTrade.GetProportionedCostOrProceed(assignmentQty);
            // If there is mutiple exercise trades it doesn't matter which trade to roll up, as all trades are the same ticker and same day are treated as a sigle trade.
            tradePairSorter.LatterTrade.AttachTradeToUnderlying(premiumCost, $"Trade is created by option assignment of option on {tradePairSorter.LatterTrade.Date:d}. \n" +
                                      $"{premiumCost} is added(subtracted) to the trade amount.", TradeReason.OptionAssigned);
            if (taxYear.ToTaxYear(tradePairSorter.EarlierTrade.Date) == taxYear.ToTaxYear(tradePairSorter.LatterTrade.Date))
            {
                tradePairSorter.EarlierTrade.RefundDisposalQty(assignmentQty);
            }
            else
            {
                tradePairSorter.LatterTrade.TaxRepayList.Add(new TaxRepay(taxYear.ToTaxYear(tradePairSorter.LatterTrade.Date),
                  premiumCost, $"Sold option with ID:{tradePairSorter.EarlierTrade.Id} and it get assigned in later tax year {taxYear.ToTaxYear(tradePairSorter.LatterTrade.Date)}"));
            }
            tradeMatch = CreateTradeMatch(tradePairSorter, assignmentQty, WrappedMoney.GetBaseCurrencyZero(), WrappedMoney.GetBaseCurrencyZero(),
                $"{assignmentQty} option assigned.", taxMatchType);
        }
        AssignTradeMatch(tradePairSorter, assignmentQty, tradeMatch, tradeMatch);
    }

    private static TradeMatch CreateTradeMatch(TradePairSorter<OptionTradeTaxCalculation> tradePairSorter, decimal matchQty, WrappedMoney allowableCost,
        WrappedMoney disposalProceed, string additionalInfo, TaxMatchType taxMatchType)
    {
        return new TradeMatch
        {
            Date = DateOnly.FromDateTime(tradePairSorter.DisposalTrade.Date),
            AssetName = tradePairSorter.DisposalTrade.AssetName,
            TradeMatchType = taxMatchType,
            MatchAcquisitionQty = matchQty,
            MatchDisposalQty = matchQty,
            BaseCurrencyMatchAllowableCost = allowableCost,
            BaseCurrencyMatchDisposalProceed = disposalProceed,
            MatchedBuyTrade = tradePairSorter.AcquisitionTrade,
            MatchedSellTrade = tradePairSorter.DisposalTrade,
            AdditionalInformation = additionalInfo
        };
    }

    private static void AssignTradeMatch(TradePairSorter<OptionTradeTaxCalculation> tradePairSorter, decimal quantity, TradeMatch acquisitionTradeMatch, TradeMatch disposalTradeMatch)
    {
        if (tradePairSorter.DisposalMatchQuantity == 0 && tradePairSorter.AcquisitionMatchQuantity == 0) return;
        tradePairSorter.AcquisitionTrade.MatchQty(quantity);
        tradePairSorter.AcquisitionTrade.MatchHistory.Add(acquisitionTradeMatch);
        tradePairSorter.DisposalTrade.MatchQty(quantity);
        tradePairSorter.DisposalTrade.MatchHistory.Add(disposalTradeMatch);
        tradePairSorter.UpdateQuantity();
    }
}
