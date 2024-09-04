using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel.Futures;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

namespace InvestmentTaxCalculator.ViewModel;

public class TradeTaxCalculationViewModel(ITradeTaxCalculation TradeTaxCalculation)
{
    public int TradeId { get; } = TradeTaxCalculation.Id;
    public string AssetType { get; } = TradeTaxCalculation.AssetCatagoryType.GetDescription();
    public DateTime Date { get; } = TradeTaxCalculation.Date;
    public string AssetName { get; } = TradeTaxCalculation.AssetName;
    public string AcquisitionOrDisposal { get; } = TradeTaxCalculation.AcquisitionDisposal.GetDescription();
    public decimal Qty { get; } = TradeTaxCalculation.TotalQty;
    public decimal SameDayMatchQty => GetMatchQty(TaxMatchType.SAME_DAY);
    public decimal BedAndBreakfastMatchQty => GetMatchQty(TaxMatchType.BED_AND_BREAKFAST);
    public decimal Section104MatchQty => GetMatchQty(TaxMatchType.SECTION_104);
    public decimal CoveredShortMatchQty => GetMatchQty(TaxMatchType.SHORTCOVER);
    public decimal UnmatchedQty { get; } = TradeTaxCalculation.UnmatchedQty;
    public decimal TotalProceed { get; } = TradeTaxCalculation.TotalProceeds.Amount;
    public decimal TotalAllowableCost { get; } = TradeTaxCalculation.TotalAllowableCost.Amount;
    public WrappedMoney? ContractValue => GetContractValue();
    public decimal Gain { get; } = TradeTaxCalculation.Gain.Amount;

    private WrappedMoney? GetContractValue()
    {
        if (TradeTaxCalculation is not FutureTradeTaxCalculation) return null;
        return ((FutureTradeTaxCalculation)TradeTaxCalculation).TotalContractValue;
    }

    private decimal GetMatchQty(TaxMatchType taxMatchType)
    {
        IEnumerable<TradeMatch> tradeMatches = TradeTaxCalculation.MatchHistory.Where(tradeMatch => tradeMatch.TradeMatchType == taxMatchType);
        if (TradeTaxCalculation.AcquisitionDisposal == TradeType.ACQUISITION)
        {
            return tradeMatches.Sum(tradeMatches => tradeMatches.MatchAcquisitionQty);
        }
        else if (TradeTaxCalculation.AcquisitionDisposal == TradeType.DISPOSAL)
        {
            return tradeMatches.Sum(tradeMatches => tradeMatches.MatchDisposalQty);
        }
        else throw new NotImplementedException($"Unexpected ENUM {TradeTaxCalculation.AcquisitionDisposal}"); // Should not reach here
    }

}
