using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;
using InvestmentTaxCalculator.Parser;
using InvestmentTaxCalculator.Services;

namespace InvestmentTaxCalculator.Model.UkTaxModel.Options;

public class UkOptionTradeCalculator(UkSection104Pools section104Pools, ITradeAndCorporateActionList tradeList, ITaxYear taxYear, TradeTaxCalculationFactory tradeTaxCalculationFactory,
    ShareIdentityRegistry? shareIdentityRegistry = null, OptionCalculationSetting? optionCalculationSetting = null) : ITradeCalculator
{
    /// <summary>
    /// Corporate actions filtered to only those applicable to option trading.
    /// This ensures each calculator processes only its relevant corporate actions, preventing duplicate application.
    /// </summary>
    private List<CorporateAction> OptionRelevantCorporateActions => [.. tradeList.CorporateActions.Where(ca => ca.AppliesToAssetCategoryType is AssetCategoryType.OPTION)];

    public List<ITradeTaxCalculation> CalculateTax()
    {
        List<OptionTradeTaxCalculation> tradeTaxCalculations = [.. tradeTaxCalculationFactory.GroupOptionTrade(tradeList.OptionTrades)];
        GroupedTradeContainer<OptionTradeTaxCalculation> _tradeContainer = new(tradeTaxCalculations, OptionRelevantCorporateActions, shareIdentityRegistry);
        UkMatchingRules.ApplyUkTaxRuleSequence(MatchTrade, _tradeContainer, section104Pools);
        return [.. tradeTaxCalculations.Cast<ITradeTaxCalculation>()];
    }

    public void MatchTrade(OptionTradeTaxCalculation trade1, OptionTradeTaxCalculation trade2, TaxMatchType taxMatchType, TaxableStatus taxableStatus)
    {
        TradePairSorter<OptionTradeTaxCalculation> tradePairSorter = new(trade1, trade2);
        if (trade1.CalculationCompleted || trade2.CalculationCompleted) return;
        if (trade1.UnmatchedQty == 0 || trade2.UnmatchedQty == 0) return;

        decimal assignmentQty, expiredQty, exercisedQty;
        // This part of the algo handle the case when you trade an option at the date of expiry and you traded/it expires/it is assigned/you exercise it in the same day
        if (tradePairSorter.LatterTrade.UnmatchedQty < tradePairSorter.EarlierTrade.UnmatchedQty)
        {
            assignmentQty = tradePairSorter.LatterTrade.AssignedQty * tradePairSorter.LatterTrade.UnmatchedQty / tradePairSorter.LatterTrade.TotalQty;
            expiredQty = tradePairSorter.LatterTrade.ExpiredQty * tradePairSorter.LatterTrade.UnmatchedQty / tradePairSorter.LatterTrade.TotalQty;
            exercisedQty = tradePairSorter.LatterTrade.OwnerExercisedQty * tradePairSorter.LatterTrade.UnmatchedQty / tradePairSorter.LatterTrade.TotalQty;
        }
        else
        {
            assignmentQty = tradePairSorter.LatterTrade.AssignedQty * tradePairSorter.EarlierTrade.UnmatchedQty / tradePairSorter.LatterTrade.TotalQty;
            expiredQty = tradePairSorter.LatterTrade.ExpiredQty * tradePairSorter.EarlierTrade.UnmatchedQty / tradePairSorter.LatterTrade.TotalQty;
            exercisedQty = tradePairSorter.LatterTrade.OwnerExercisedQty * tradePairSorter.EarlierTrade.UnmatchedQty / tradePairSorter.LatterTrade.TotalQty;
        }
        if (assignmentQty > 0 && exercisedQty > 0) throw new ParseException($"{tradePairSorter.LatterTrade} has both assignment and exercise which is impossible");
        if (expiredQty > 0) MatchExpiredOption(tradePairSorter, taxMatchType, expiredQty, taxableStatus);
        if (exercisedQty > 0) MatchExercisedOption(tradePairSorter, taxMatchType, exercisedQty, taxableStatus);
        if (assignmentQty > 0) MatchAssignedOption(tradePairSorter, taxMatchType, assignmentQty, taxableStatus);
        if (tradePairSorter.DisposalMatchQuantity > 0) MatchNormalTrade(tradePairSorter, taxMatchType, taxableStatus);
    }

    private void MatchNormalTrade(TradePairSorter<OptionTradeTaxCalculation> tradePairSorter, TaxMatchType taxMatchType, TaxableStatus taxableStatus)
    {
        WrappedMoney allowableCost = tradePairSorter.AcquisitionTrade.GetProportionedCostOrProceedForTradeReason(TradeReason.OrderedTrade, tradePairSorter.AcquisitionMatchQuantity);
        WrappedMoney disposalProceed = tradePairSorter.DisposalTrade.GetProportionedCostOrProceedForTradeReason(TradeReason.OrderedTrade, tradePairSorter.DisposalMatchQuantity);
        // If written an option and the position is held to next tax year, full disposal proceed is taxed and allowable cost can only be reclaimed when the trade is closed.
        if (tradePairSorter.EarlierTrade.AcquisitionDisposal == TradeType.DISPOSAL && RefundIfNotInSameYear(tradePairSorter, allowableCost, "position closed"))
        {
            allowableCost = WrappedMoney.GetBaseCurrencyZero();
        }
        TradeMatch disposalTradeMatch = CreateTradeMatch(tradePairSorter, tradePairSorter.AcquisitionMatchQuantity,
            allowableCost, disposalProceed, string.Empty, taxMatchType, taxableStatus);
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
    private static void MatchExpiredOption(TradePairSorter<OptionTradeTaxCalculation> tradePairSorter, TaxMatchType taxMatchType, decimal expiredQty, TaxableStatus taxableStatus)
    {
        TradeMatch disposalTradeMatch;
        // You sold an option and it expires
        if (tradePairSorter.EarlierTrade.AcquisitionDisposal == TradeType.DISPOSAL)
        {
            disposalTradeMatch = CreateTradeMatch(tradePairSorter, expiredQty, WrappedMoney.GetBaseCurrencyZero(),
            tradePairSorter.DisposalTrade.GetProportionedCostOrProceed(expiredQty), "The granted option expired. No allowable cost is added.", taxMatchType, taxableStatus);
        }
        // You bought an option and it expires
        else
        {
            disposalTradeMatch = CreateTradeMatch(tradePairSorter, expiredQty, tradePairSorter.AcquisitionTrade.GetProportionedCostOrProceed(expiredQty),
            tradePairSorter.DisposalTrade.GetProportionedCostOrProceedForTradeReason(TradeReason.Expired, expiredQty), "The bought option expired. Full premium is added to allowable cost.", taxMatchType, taxableStatus);
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
    private static void MatchExercisedOption(TradePairSorter<OptionTradeTaxCalculation> tradePairSorter, TaxMatchType taxMatchType, decimal exercisedQty, TaxableStatus taxableStatus)
    {
        TradeMatch tradeMatch;
        if (tradePairSorter.LatterTrade.SettlementMethod is SettlementMethods.CASH)
        {
            WrappedMoney allowableCost = tradePairSorter.AcquisitionTrade.GetProportionedCostOrProceedForTradeReason(TradeReason.OrderedTrade, exercisedQty);
            WrappedMoney disposalProceed = tradePairSorter.DisposalTrade.GetProportionedCostOrProceedForTradeReason(TradeReason.OwnerExerciseOption, exercisedQty);
            tradeMatch = CreateTradeMatch(tradePairSorter, exercisedQty, allowableCost, disposalProceed, $"{exercisedQty:F2} option cash settled.", taxMatchType, taxableStatus);
        }
        else if (tradePairSorter.LatterTrade.SettlementMethod is SettlementMethods.DELIVERY)
        {
            WrappedMoney premiumCost = tradePairSorter.EarlierTrade.GetProportionedCostOrProceed(exercisedQty);
            WrappedMoney exerciseCost = tradePairSorter.LatterTrade.GetSettlementTransactionCost(exercisedQty);
            // If there is multiple exercise trades it doesn't matter which trade to roll up, as all trades are the same ticker and same day are treated as a single trade.
            tradePairSorter.LatterTrade.AttachTradeToUnderlying(premiumCost + exerciseCost, $"Option premium adjustment due to exercising option", TradeReason.OwnerExerciseOption);
            tradeMatch = CreateTradeMatch(tradePairSorter, exercisedQty, WrappedMoney.GetBaseCurrencyZero(), WrappedMoney.GetBaseCurrencyZero(), $"{exercisedQty} option exercised.", taxMatchType, taxableStatus);
        }
        else throw new InvalidDataException($"Unexpected settlement method {tradePairSorter.LatterTrade.SettlementMethod}");
        AssignTradeMatch(tradePairSorter, exercisedQty, tradeMatch, tradeMatch);
    }

    /// <summary>
    /// You sold an option and get an assignment.
    /// The option trade get rolled up to the assignment acquisition and disposal of the underlying and the option trade have no tax effect.
    /// Call option: sale proceed = sale proceed for the underlying + premium already received
    /// Put option: allowable cost = allowable cost for buying the underlying - premium already received
    /// </summary>
    private void MatchAssignedOption(TradePairSorter<OptionTradeTaxCalculation> tradePairSorter, TaxMatchType taxMatchType, decimal assignmentQty, TaxableStatus taxableStatus)
    {
        TradeMatch tradeMatch;
        if (tradePairSorter.LatterTrade.SettlementMethod is SettlementMethods.CASH)
        {
            WrappedMoney allowableCost = tradePairSorter.AcquisitionTrade.GetProportionedCostOrProceedForTradeReason(TradeReason.OptionAssigned, assignmentQty);
            WrappedMoney disposalProceed = tradePairSorter.DisposalTrade.GetProportionedCostOrProceedForTradeReason(TradeReason.OrderedTrade, assignmentQty);
            if (RefundIfNotInSameYear(tradePairSorter, allowableCost, "it get assigned"))
            {
                allowableCost = WrappedMoney.GetBaseCurrencyZero();
            }
            tradeMatch = CreateTradeMatch(tradePairSorter, assignmentQty, allowableCost, disposalProceed, $"{assignmentQty:F2} option is cash settled.", taxMatchType, taxableStatus);
        }
        else if (tradePairSorter.LatterTrade.SettlementMethod is SettlementMethods.DELIVERY)
        {
            WrappedMoney premiumReceived = tradePairSorter.EarlierTrade.GetProportionedCostOrProceed(assignmentQty);
            WrappedMoney assignmentCost = tradePairSorter.LatterTrade.GetSettlementTransactionCost(assignmentQty);
            WrappedMoney netPremiumReceived = premiumReceived + assignmentCost;
            // If there is multiple exercise trades it doesn't matter which trade to roll up, as all trades are the same ticker and same day are treated as a single trade.
            tradePairSorter.LatterTrade.AttachTradeToUnderlying(netPremiumReceived, $"Option premium adjustment due to option assignment. " +
                $"Premium received {premiumReceived}, assignment cost {assignmentCost}", TradeReason.OptionAssigned);
            // The trade is as if not happened when an option is assigned, so in previous year you are assessed premiumReceived, so that is refunded,
            // but netPremiumReceived is added to the underlying trade
            if (!RefundIfNotInSameYear(tradePairSorter, premiumReceived, "it get assigned"))
            {
                tradePairSorter.EarlierTrade.RefundDisposalQty(assignmentQty);
            }
            tradeMatch = CreateTradeMatch(tradePairSorter, assignmentQty, WrappedMoney.GetBaseCurrencyZero(), WrappedMoney.GetBaseCurrencyZero(),
                $"{assignmentQty} option assigned. Option premium is carried over to trade of the underlying asset and no tax is assessed for this match", taxMatchType, taxableStatus);
        }
        else throw new InvalidDataException($"Unexpected settlement method {tradePairSorter.LatterTrade.SettlementMethod}");
        AssignTradeMatch(tradePairSorter, assignmentQty, tradeMatch, tradeMatch);
    }

    /// <summary>
    /// If a written option is closed in a later tax year, list the closing cost as a tax refund for the tax year the option was written.
    /// Returns false if the option is closed in the same tax year or the user chose to deduct the closing cost in the tax year the option was written,
    /// in which case the closing cost should be deducted normally.
    /// </summary>
    private bool RefundIfNotInSameYear(TradePairSorter<OptionTradeTaxCalculation> tradePairSorter, WrappedMoney refundAmount, string closingDescription)
    {
        if (optionCalculationSetting?.DeductClosingCostInWrittenTaxYear is true) return false;
        int writtenTaxYear = taxYear.ToTaxYear(tradePairSorter.EarlierTrade.Date);
        int closingTaxYear = taxYear.ToTaxYear(tradePairSorter.LatterTrade.Date);
        if (writtenTaxYear == closingTaxYear) return false;
        tradePairSorter.DisposalTrade.TaxRepayList.Add(
            new TaxRepay(
            WrittenTaxYear: writtenTaxYear,
            ClosingTaxYear: closingTaxYear,
            RefundAmount: refundAmount,
            Reason: $"Sold option {tradePairSorter.EarlierTrade.AssetName} " +
            $"with ID:{tradePairSorter.EarlierTrade.Id} on {tradePairSorter.EarlierTrade.Date:d} " +
            $"and {closingDescription} in later tax year with trade ID:{tradePairSorter.LatterTrade.Id}"
            ));
        return true;
    }

    private static TradeMatch CreateTradeMatch(TradePairSorter<OptionTradeTaxCalculation> tradePairSorter, decimal matchQty, WrappedMoney allowableCost,
        WrappedMoney disposalProceed, string additionalInfo, TaxMatchType taxMatchType, TaxableStatus taxableStatus)
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
            AdditionalInformation = additionalInfo,
            IsTaxable = taxableStatus
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
