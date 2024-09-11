using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Model.UkTaxModel;

public class MatchAdjustment
{
    /// <summary>
    /// Indicate how the quantity of the shares matched should be adjusted after corporate action(s).
    /// trade 1 + trade 2: N shares of the earlier trade should be matched with N * MatchAdjustmentFactor of the latter trade
    /// </summary>
    public decimal MatchAdjustmentFactor { get; set; } = 1;
    public List<CorporateAction> CorporateActions { get; init; } = [];
}
