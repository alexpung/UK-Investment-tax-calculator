using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;

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
    /// <summary>
    /// Corporate actions filtered to only those applicable to stock and FX trading.
    /// This ensures each calculator processes only its relevant corporate actions, preventing duplicate application.
    /// </summary>
    private List<CorporateAction> StockRelevantCorporateActions => [.. tradeList.CorporateActions.Where(ca => ca.AppliesToAssetCategoryType is AssetCategoryType.STOCK)];

    public List<ITradeTaxCalculation> CalculateTax()
    {
        List<ITradeTaxCalculation> tradeTaxCalculations = [.. tradeTaxCalculationFactory.GroupTrade(tradeList.Trades)];
        
        // Clear previously generated disposals to ensure a fresh calculation
        foreach (var ca in StockRelevantCorporateActions)
        {
            ca.GeneratedDisposals.Clear();
        }

        GroupedTradeContainer<ITradeTaxCalculation> _tradeContainer = new(tradeTaxCalculations, StockRelevantCorporateActions);
        UkMatchingRules.ApplyUkTaxRuleSequence(MatchTrade, _tradeContainer, section104Pools);

        // Collect generated disposals from corporate actions (e.g., cash from takeovers/spinoffs/splits)
        var generatedDisposals = StockRelevantCorporateActions.SelectMany(ca => ca.GeneratedDisposals);
        tradeTaxCalculations.AddRange(generatedDisposals);
        return tradeTaxCalculations;
    }

    public void MatchTrade(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, TaxMatchType taxMatchType, TaxableStatus taxableStatus)
    {
        TradePairSorter<ITradeTaxCalculation> tradePairSorter = new(trade1, trade2);
        if (trade1.CalculationCompleted || trade2.CalculationCompleted) return;
        MatchAdjustment matchAdjustment = StockRelevantCorporateActions
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
            AdditionalInformation = BuildInfoString(matchAdjustment.CorporateActions),
            IsTaxable = taxableStatus,
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
