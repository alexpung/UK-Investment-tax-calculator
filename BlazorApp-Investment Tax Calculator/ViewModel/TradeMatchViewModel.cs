using Enumerations;

using Model.UkTaxModel.Stocks;

namespace ViewModel;

public record TradeMatchViewModel(TradeMatch tradeMatch)
{
    public DateOnly DisposalDate { get; } = tradeMatch.Date;
    public string? AssetType { get; } = tradeMatch.MatchedSellTrade?.AssetCatagoryType.GetDescription() ?? tradeMatch.MatchedBuyTrade?.AssetCatagoryType.GetDescription();
    public string AssetName { get; } = tradeMatch.AssetName;
    public string MatchType { get; } = tradeMatch.TradeMatchType.GetDescription();
    public int? AcquistionTradeId { get; } = tradeMatch.MatchedBuyTrade?.Id;
    public int? DisposalTradeId { get; } = tradeMatch.MatchedSellTrade?.Id;
    public decimal MatchDisposalQty { get; } = tradeMatch.MatchDisposalQty;
    public decimal DisposalProceed { get; } = tradeMatch.BaseCurrencyMatchDisposalProceed.Amount;
    public decimal AllowableCost { get; } = tradeMatch.BaseCurrencyMatchAllowableCost.Amount;
    public decimal Gain { get; } = tradeMatch.MatchGain.Amount;

}
