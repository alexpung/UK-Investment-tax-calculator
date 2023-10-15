using Model.TaxEvents;

namespace Model.Interfaces;
public interface ITradeAndCorporateActionList
{
    List<CorporateAction> CorporateActions { get; set; }
    List<Trade> Trades { get; set; }
}