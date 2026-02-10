using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;

using System.Text.Json.Serialization;

namespace InvestmentTaxCalculator.Model.TaxEvents;

[JsonPolymorphic()]
[JsonDerivedType(typeof(StockSplit), "stockSplit")]
[JsonDerivedType(typeof(ExcessReportableIncome), "eri")]
[JsonDerivedType(typeof(FundEqualisation), "fundEqualisation")]
[JsonDerivedType(typeof(TakeoverCorporateAction), "takeover")]
[JsonDerivedType(typeof(ReturnOfCapitalCorporateAction), "roc")]
[JsonDerivedType(typeof(SpinoffCorporateAction), "spinoff")]
public abstract record CorporateAction : TaxEvent
{
    /// <summary>
    /// Ordered list of company tickers that this corporate action affects.
    /// Earlier tickers in the list must be processed before later tickers.
    /// </summary>
    public virtual IReadOnlyList<string> CompanyTickersInProcessingOrder => [AssetName];
    public abstract MatchAdjustment TradeMatching(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, MatchAdjustment matchAdjustment);
    public abstract void ChangeSection104(UkSection104 section104);
    public virtual string Reason => "";
    public override string ToSummaryString() => $"Corporate Action: {AssetName} ({Date.ToShortDateString()}) - {Reason}";
    public abstract AssetCategoryType AppliesToAssetCategoryType { get; }
}
