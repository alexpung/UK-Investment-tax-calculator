using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

namespace InvestmentTaxCalculator.ViewModel;

public record TradeMatchViewModel(TradeMatch TradeMatch)
{
    public DateOnly DisposalDate { get; } = TradeMatch.Date;
    public string? AssetType { get; } = TradeMatch.MatchedSellTrade?.AssetCategoryType.GetDescription() ?? TradeMatch.MatchedBuyTrade?.AssetCategoryType.GetDescription();
    public string AssetName { get; } = TradeMatch.AssetName;
    public string MatchType { get; } = TradeMatch.TradeMatchType.GetDescription();
    public int? AcquisitionTradeId { get; } = TradeMatch.MatchedBuyTrade?.Id;
    public int? DisposalTradeId { get; } = TradeMatch.MatchedSellTrade?.Id;
    public decimal MatchAcquisitionQty { get; } = TradeMatch.MatchAcquisitionQty;
    public decimal MatchDisposalQty { get; } = TradeMatch.MatchDisposalQty;
    public decimal DisposalProceed { get; } = TradeMatch.BaseCurrencyMatchDisposalProceed.Amount;
    public decimal AllowableCost { get; } = TradeMatch.BaseCurrencyMatchAllowableCost.Amount;
    public decimal Gain { get; } = TradeMatch.MatchGain.Amount;
    public string AdditionalInformation { get; } = TradeMatch.AdditionalInformation;

}
