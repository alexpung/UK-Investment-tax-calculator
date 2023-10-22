using Enum;
using Model.Interfaces;

namespace Model.UkTaxModel;

public record UkSection104
{
    public string AssetName { get; init; }
    public decimal Quantity { get; set; }
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
