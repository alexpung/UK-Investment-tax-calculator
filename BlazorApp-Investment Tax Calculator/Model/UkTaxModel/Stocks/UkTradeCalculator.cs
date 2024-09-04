using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel.Fx;

using Syncfusion.Blazor.Data;

namespace InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

/// <summary>
/// Calculate Fx and stock trades
/// </summary>
/// <param name="section104Pools"></param>
/// <param name="tradeList"></param>
public class UkTradeCalculator(UkSection104Pools section104Pools, ITradeAndCorporateActionList tradeList) : ITradeCalculator
{
    public List<ITradeTaxCalculation> CalculateTax()
    {
        List<ITradeTaxCalculation> tradeTaxCalculations = [.. GroupTrade(tradeList.Trades)];
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

    private static List<TradeTaxCalculation> GroupTrade(IEnumerable<Trade> trades)
    {
        var groupedTrade = from trade in trades
                           where trade.AssetType == AssetCatagoryType.STOCK
                           group trade by new { trade.AssetName, trade.Date.Date, trade.AcquisitionDisposal };
        var groupedFxTrade = from trade in trades
                             where trade.AssetType == AssetCatagoryType.FX
                             group trade by new { trade.AssetName, trade.Date.Date, trade.AcquisitionDisposal };
        IEnumerable<TradeTaxCalculation> groupedTradeCalculations = groupedTrade.Select(group => new TradeTaxCalculation(group)).ToList();
        IEnumerable<TradeTaxCalculation> groupedFxTradeCalculations = groupedFxTrade.Select(group => new FxTradeTaxCalculation(group)).ToList();
        return groupedTradeCalculations.Concat(groupedFxTradeCalculations).ToList();
    }

    public void MatchTrade(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, TaxMatchType taxMatchType)
    {
        TradePairSorter tradePairSorter = new(trade1, trade2);
        if (trade1.CalculationCompleted || trade2.CalculationCompleted) return;
        MatchAdjustment matchAdjustment = tradeList.CorporateActions
            .Aggregate(new MatchAdjustment(), (matchAdjustment, corporateAction) => corporateAction.TradeMatching(trade1, trade2, matchAdjustment));
        decimal proposedMatchQuantity = Math.Min(tradePairSorter.EarlierTrade.UnmatchedQty, tradePairSorter.LatterTrade.UnmatchedQty / matchAdjustment.MatchAdjustmentFactor);
        decimal acqusitionMatchQuantity = tradePairSorter.EarlierTrade.AcquisitionDisposal == TradeType.ACQUISITION ? proposedMatchQuantity : proposedMatchQuantity * matchAdjustment.MatchAdjustmentFactor;
        decimal disposalMatchQuantity = tradePairSorter.EarlierTrade.AcquisitionDisposal == TradeType.DISPOSAL ? proposedMatchQuantity : proposedMatchQuantity * matchAdjustment.MatchAdjustmentFactor;
        TradeMatch disposalTradeMatch = new()
        {
            Date = DateOnly.FromDateTime(tradePairSorter.DisposalTrade.Date),
            AssetName = tradePairSorter.DisposalTrade.AssetName,
            TradeMatchType = taxMatchType,
            MatchAcquisitionQty = acqusitionMatchQuantity,
            MatchDisposalQty = disposalMatchQuantity,
            BaseCurrencyMatchAllowableCost = tradePairSorter.AcqusitionTrade.GetProportionedCostOrProceed(acqusitionMatchQuantity),
            BaseCurrencyMatchDisposalProceed = tradePairSorter.DisposalTrade.GetProportionedCostOrProceed(disposalMatchQuantity),
            MatchedBuyTrade = tradePairSorter.AcqusitionTrade,
            MatchedSellTrade = tradePairSorter.DisposalTrade,
            AdditionalInformation = matchAdjustment.CorporateActions.ToString() ?? ""
        };
        TradeMatch AcqusitionTradeMatch = disposalTradeMatch with
        {
            BaseCurrencyMatchAllowableCost = WrappedMoney.GetBaseCurrencyZero(),
            BaseCurrencyMatchDisposalProceed = WrappedMoney.GetBaseCurrencyZero(),
        };
        tradePairSorter.AcqusitionTrade.MatchQty(acqusitionMatchQuantity);
        tradePairSorter.DisposalTrade.MatchQty(disposalMatchQuantity);
        tradePairSorter.AcqusitionTrade.MatchHistory.Add(AcqusitionTradeMatch);
        tradePairSorter.DisposalTrade.MatchHistory.Add(disposalTradeMatch);
    }
}

public record TradePairSorter
{
    public ITradeTaxCalculation EarlierTrade { get; init; }
    public ITradeTaxCalculation LatterTrade { get; init; }
    public ITradeTaxCalculation DisposalTrade { get; init; }
    public ITradeTaxCalculation AcqusitionTrade { get; init; }

    public TradePairSorter(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2)
    {
        if (!(
            (trade1.AcquisitionDisposal == TradeType.ACQUISITION && trade2.AcquisitionDisposal == TradeType.DISPOSAL) ||
            (trade1.AcquisitionDisposal == TradeType.DISPOSAL && trade2.AcquisitionDisposal == TradeType.ACQUISITION)
            ))
        {
            throw new ArgumentException("The provided trades should consist of one buy and one sell trade.");
        }
        EarlierTrade = trade1.Date > trade2.Date ? trade2 : trade1;
        LatterTrade = trade1.Date > trade2.Date ? trade1 : trade2;
        DisposalTrade = trade1.AcquisitionDisposal == TradeType.DISPOSAL ? trade1 : trade2;
        AcqusitionTrade = trade1.AcquisitionDisposal == TradeType.ACQUISITION ? trade1 : trade2;
    }
}
