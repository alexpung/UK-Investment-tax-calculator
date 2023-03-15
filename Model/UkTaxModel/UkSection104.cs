using CapitalGainCalculator.Enum;
using CapitalGainCalculator.Model.Interfaces;
using System;
using System.Collections.Generic;

namespace CapitalGainCalculator.Model.UkTaxModel;

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
    public decimal ValueInBaseCurrency { get; private set; }
    public List<Section104History> Section104HistoryList { get; private set; } = new();

    public UkSection104(string name)
    {
        AssetName = name;
        Quantity = 0m;
        ValueInBaseCurrency = 0m;
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

    public void ShareAdjustment(StockSplit stockSplit)
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
        (decimal qty, decimal value) = tradeTaxCalculation.MatchAll();
        tradeTaxCalculation.MatchHistory.Add(CreateUkMatchHistory(qty, value));
        Section104HistoryList.Add(Section104History.AddToSection104(tradeTaxCalculation, qty, value, Quantity, ValueInBaseCurrency));
        Quantity += qty;
        ValueInBaseCurrency += value;
    }

    private void RemoveAssets(ITradeTaxCalculation tradeTaxCalculation)
    {
        decimal qty, disposalValue, acqisitionValue;
        if (Quantity == 0m) return;
        if (tradeTaxCalculation.UnmatchedQty <= Quantity)
        {
            (qty, disposalValue) = tradeTaxCalculation.MatchAll();

        }
        else
        {
            (qty, disposalValue) = tradeTaxCalculation.MatchQty(Quantity);
        }
        acqisitionValue = decimal.Round(qty / Quantity * ValueInBaseCurrency, 2);
        tradeTaxCalculation.MatchHistory.Add(CreateUkMatchHistory(qty, acqisitionValue, disposalValue));
        Section104HistoryList.Add(Section104History.RemoveFromSection104(tradeTaxCalculation, qty * -1, acqisitionValue * -1, Quantity, ValueInBaseCurrency));
        Quantity -= qty;
        ValueInBaseCurrency -= acqisitionValue;
    }

    private static TradeMatch CreateUkMatchHistory(decimal qty, decimal acqisitionValue, decimal disposalValue = 0)
    {
        return new()
        {
            TradeMatchType = UkMatchType.SECTION_104,
            MatchQuantity = qty,
            BaseCurrencyMatchAcquitionValue = acqisitionValue,
            BaseCurrencyMatchDisposalValue = disposalValue
        };
    }
}
