using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Model.Interfaces;
public interface ITradeAndCorporateActionList
{
    List<CorporateAction> CorporateActions { get; set; }
    List<Trade> Trades { get; set; }
    public List<OptionTrade> OptionTrades { get; set; }
    public List<FutureContractTrade> FutureContractTrades { get; set; }
    public List<CashSettlement> CashSettlements { get; set; }
}