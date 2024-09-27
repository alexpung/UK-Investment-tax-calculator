using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;

namespace InvestmentTaxCalculator.Model.TaxEvents;

public record StockSplit : CorporateAction, IChangeSection104
{
    /// <summary>
    /// The number of shares after the split that are being given for the original number of shares.
    /// For example, in a 2:1 stock split, where 2 shares are given for every 1 share, this is 2.
    /// </summary>
    public required int SplitTo { get; init; }
    /// <summary>
    /// The number of shares prior to the split. 
    /// For example, in a 2:1 stock split, where 2 shares are given for every 1 share, this is 1.
    /// </summary>
    public required int SplitFrom { get; init; }
    public override string Reason => $"{AssetName} undergo a stock split {SplitTo} for {SplitFrom} on {Date:d}\n";

    public override MatchAdjustment TradeMatching(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, MatchAdjustment matchAdjustment)
    {
        ITradeTaxCalculation earlierTrade = trade1.Date <= trade2.Date ? trade1 : trade2;
        ITradeTaxCalculation laterTrade = trade1.Date > trade2.Date ? trade1 : trade2;
        if (AssetName != trade1.AssetName || AssetName != trade2.AssetName) return matchAdjustment;
        if (earlierTrade.Date > Date || Date > laterTrade.Date) return matchAdjustment;
        matchAdjustment.MatchAdjustmentFactor *= (decimal)SplitTo / SplitFrom;
        matchAdjustment.CorporateActions.Add(this);
        return matchAdjustment;
    }

    public override void ChangeSection104(UkSection104 section104)
    {
        if (AssetName != section104.AssetName) return;
        decimal newQuantity = section104.Quantity * SplitTo / SplitFrom;
        section104.Section104HistoryList.Add(Section104History.ShareAdjustment(Date, section104.Quantity, newQuantity));
        section104.Quantity = newQuantity;
    }
}
