using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;

using System.Text.Json.Serialization;

namespace InvestmentTaxCalculator.Model.TaxEvents;

[JsonDerivedType(typeof(StockSplit), "stockSplit")]
public abstract record CorporateAction : TaxEvent
{
    public abstract MatchAdjustment TradeMatching(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, MatchAdjustment matchAdjustment);
    public abstract void ChangeSection104(UkSection104 section104);
}
