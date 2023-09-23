using Enum;
using Model.Interfaces;

namespace Model.UkTaxModel;

public record UkSection104
{
    public string AssetName { get; init; }
    private decimal _quantity;
    public decimal Quantity
    {
        get { return _quantity; }
        private set
        {
            if (value < 0) throw new ArgumentOutOfRangeException($"Section 104 cannot go below zero. Current status {this}, new value to be set {value}");
            else _quantity = value;
        }
    }
    public WrappedMoney ValueInBaseCurrency { get; private set; }
    public List<Section104History> Section104HistoryList { get; private set; } = new();

    public UkSection104(string name)
    {
        AssetName = name;
        Quantity = 0m;
        ValueInBaseCurrency = WrappedMoney.GetBaseCurrencyZero();
    }

    public void MatchTradeWithSection104(ITradeTaxCalculation tradeTaxCalculation)
    {
        if (tradeTaxCalculation.BuySell == TradeType.BUY)
        {
            AddAssets(tradeTaxCalculation);
        }
        else if (tradeTaxCalculation.BuySell == TradeType.SELL)
        {
            RemoveAssets(tradeTaxCalculation);
        }
        else throw new ArgumentException($"Unknown BuySell Type {tradeTaxCalculation.BuySell}");
    }

    public void PerformCorporateAction(CorporateAction action)
    {
        switch (action)
        {
            case StockSplit:
                ShareAdjustment((StockSplit)action);
                break;
            default:
                throw new NotImplementedException($"{action} corporate action not implemented!");
        }
    }

    private void ShareAdjustment(StockSplit stockSplit)
    {
        decimal newQuantity = stockSplit.GetSharesAfterSplit(Quantity);
        Section104HistoryList.Add(Section104History.ShareAdjustment(stockSplit.Date, Quantity, newQuantity));
        Quantity = newQuantity;
    }

    private void AddAssets(ITradeTaxCalculation tradeTaxCalculation)
    {
        if (tradeTaxCalculation.UnmatchedQty < 0)
        {
            throw new ArgumentOutOfRangeException
                ($"Cannot add assets with negative quantity {tradeTaxCalculation.UnmatchedQty} and value {tradeTaxCalculation.UnmatchedNetAmount}");
        }
        (decimal qty, WrappedMoney value) = tradeTaxCalculation.MatchAll();
        Section104History newSection104History = Section104History.AddToSection104(tradeTaxCalculation, qty, value, Quantity, ValueInBaseCurrency);
        tradeTaxCalculation.MatchHistory.Add(TradeMatch.CreateSection104Match(qty, value, WrappedMoney.GetBaseCurrencyZero(), newSection104History));
        Section104HistoryList.Add(newSection104History);
        Quantity += qty;
        ValueInBaseCurrency += value;
    }

    private void RemoveAssets(ITradeTaxCalculation tradeTaxCalculation)
    {
        decimal qty;
        WrappedMoney disposalValue, acqisitionValue;
        if (Quantity == 0m) return;
        if (tradeTaxCalculation.UnmatchedQty <= Quantity)
        {
            (qty, disposalValue) = tradeTaxCalculation.MatchAll();

        }
        else
        {
            (qty, disposalValue) = tradeTaxCalculation.MatchQty(Quantity);
        }
        acqisitionValue = ValueInBaseCurrency * qty / Quantity;
        Section104History newSection104History = Section104History.RemoveFromSection104(tradeTaxCalculation, qty * -1, acqisitionValue * -1, Quantity, ValueInBaseCurrency);
        tradeTaxCalculation.MatchHistory.Add(TradeMatch.CreateSection104Match(qty, acqisitionValue, disposalValue, newSection104History));
        Section104HistoryList.Add(newSection104History);
        Quantity -= qty;
        ValueInBaseCurrency -= acqisitionValue;
    }
}
