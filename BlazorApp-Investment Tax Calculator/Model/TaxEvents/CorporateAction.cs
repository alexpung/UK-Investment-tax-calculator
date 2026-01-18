using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;

using System.Text.Json.Serialization;

namespace InvestmentTaxCalculator.Model.TaxEvents;

[JsonPolymorphic()]
[JsonDerivedType(typeof(StockSplit), "stockSplit")]
[JsonDerivedType(typeof(ExcessReportableIncome), "eri")]
[JsonDerivedType(typeof(FundEqualisation), "fundEqualisation")]
public abstract record CorporateAction : TaxEvent
{
    public abstract MatchAdjustment TradeMatching(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, MatchAdjustment matchAdjustment);
    public abstract void ChangeSection104(UkSection104 section104);
    public virtual string Reason => "";
    public override string ToSummaryString() => $"Corporate Action: {AssetName} ({Date.ToShortDateString()}) - {Reason}";
}
