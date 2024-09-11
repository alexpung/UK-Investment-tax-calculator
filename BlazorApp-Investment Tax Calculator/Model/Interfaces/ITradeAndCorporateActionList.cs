using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Model.Interfaces;
public interface ITradeAndCorporateActionList
{
    List<CorporateAction> CorporateActions { get; set; }
    List<Trade> Trades { get; set; }
}