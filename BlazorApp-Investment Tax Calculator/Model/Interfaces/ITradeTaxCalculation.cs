using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

namespace InvestmentTaxCalculator.Model.Interfaces;

/// <summary>
/// A ITradeTaxCalculation contains a list of trades that are grouped together and matched for tax calculation
/// </summary>
public interface ITradeTaxCalculation : ITextFilePrintable, ITaxMatchable
{
    int Id { get; }
    AssetCategoryType AssetCategoryType { get; }
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
    ResidencyStatus ResidencyStatusAtTrade { get; set; }
    WrappedMoney GetProportionedCostOrProceed(decimal qty) => TotalCostOrProceed / TotalQty * qty;
    void MatchWithSection104(UkSection104 ukSection104, TaxableStatus taxableStatus);
    void MatchQty(decimal demandedQty);
    DateTime TaxableDate { get; set; }
}