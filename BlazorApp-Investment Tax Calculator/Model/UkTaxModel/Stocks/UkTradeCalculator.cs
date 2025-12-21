using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel.Fx;

using Syncfusion.Blazor.Data;

using System.Text;

namespace InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

/// <summary>
/// Calculate Fx and stock trades
/// </summary>
/// <param name="section104Pools"></param>
/// <param name="tradeList"></param>
public class UkTradeCalculator(UkSection104Pools section104Pools, ITradeAndCorporateActionList tradeList, TradeTaxCalculationFactory tradeTaxCalculationFactory) : ITradeCalculator
{
    public List<ITradeTaxCalculation> CalculateTax()
    {
        List<ITradeTaxCalculation> tradeTaxCalculations = [.. tradeTaxCalculationFactory.GroupTrade(tradeList.Trades)];
        GroupedTradeContainer<ITradeTaxCalculation> _tradeContainer = new(tradeTaxCalculations, tradeList.CorporateActions);
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

        return tradeTaxCalculations;
    }

    public void MatchTrade(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, TaxMatchType taxMatchType)
    {
        TradePairSorter<ITradeTaxCalculation> tradePairSorter = new(trade1, trade2);
        if (trade1.CalculationCompleted || trade2.CalculationCompleted) return;
        MatchAdjustment matchAdjustment = tradeList.CorporateActions
            .Aggregate(new MatchAdjustment(), (matchAdjustment, corporateAction) => corporateAction.TradeMatching(trade1, trade2, matchAdjustment));
        tradePairSorter.SetQuantityAdjustmentFactor(matchAdjustment.MatchAdjustmentFactor);
        TradeMatch disposalTradeMatch = new()
        {
            Date = DateOnly.FromDateTime(tradePairSorter.DisposalTrade.Date),
            AssetName = tradePairSorter.DisposalTrade.AssetName,
            TradeMatchType = taxMatchType,
            MatchAcquisitionQty = tradePairSorter.AcquisitionMatchQuantity,
            MatchDisposalQty = tradePairSorter.DisposalMatchQuantity,
            BaseCurrencyMatchAllowableCost = tradePairSorter.AcquisitionTrade.GetProportionedCostOrProceed(tradePairSorter.AcquisitionMatchQuantity),
            BaseCurrencyMatchDisposalProceed = tradePairSorter.DisposalTrade.GetProportionedCostOrProceed(tradePairSorter.DisposalMatchQuantity),
            MatchedBuyTrade = tradePairSorter.AcquisitionTrade,
            MatchedSellTrade = tradePairSorter.DisposalTrade,
            AdditionalInformation = BuildInfoString(matchAdjustment.CorporateActions)
        };
        TradeMatch AcqusitionTradeMatch = disposalTradeMatch with
        {
            BaseCurrencyMatchAllowableCost = WrappedMoney.GetBaseCurrencyZero(),
            BaseCurrencyMatchDisposalProceed = WrappedMoney.GetBaseCurrencyZero(),
        };
        tradePairSorter.AcquisitionTrade.MatchQty(tradePairSorter.AcquisitionMatchQuantity);
        tradePairSorter.DisposalTrade.MatchQty(tradePairSorter.DisposalMatchQuantity);
        tradePairSorter.AcquisitionTrade.MatchHistory.Add(AcqusitionTradeMatch);
        tradePairSorter.DisposalTrade.MatchHistory.Add(disposalTradeMatch);
    }

    private static string BuildInfoString(List<CorporateAction> corporateActions)
    {
        StringBuilder sb = new();
        foreach (var action in corporateActions)
        {
            sb.AppendLine(action.Reason.ToString());
        }
        return sb.ToString();
    }
}
