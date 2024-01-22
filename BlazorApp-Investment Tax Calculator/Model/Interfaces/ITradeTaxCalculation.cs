using Enumerations;

using Model.TaxEvents;
using Model.UkTaxModel;
using Model.UkTaxModel.Stocks;

namespace Model.Interfaces;
public interface ITradeTaxCalculation : ITextFilePrintable, IAssetDatedEvent
{
    int Id { get; }
    TradeType AcquisitionDisposal { get; init; }
    AssetCatagoryType AssetCatagoryType { get; }
    bool CalculationCompleted { get; }
    List<TradeMatch> MatchHistory { get; init; }
    WrappedMoney TotalCostOrProceed { get; }
    decimal TotalQty { get; }
    List<Trade> TradeList { get; init; }
    WrappedMoney UnmatchedCostOrProceed { get; }
    decimal UnmatchedQty { get; }
    WrappedMoney TotalProceeds { get; }
    WrappedMoney TotalAllowableCost { get; }
    WrappedMoney Gain { get; }
    WrappedMoney GetProportionedCostOrProceed(decimal qty) => TotalCostOrProceed / TotalQty * qty;
    void MatchWithSection104(UkSection104 ukSection104);
    void MatchQty(decimal demandedQty);
}