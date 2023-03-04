using CapitalGainCalculator.Enum;
using System;
using System.Collections.Generic;

namespace CapitalGainCalculator.Model.UkTaxModel
{
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

        public List<TradeTaxCalculation> MatchedTradesList { get; private set; }

        public UkSection104(string name)
        {
            AssetName = name;
            Quantity = 0m;
            ValueInBaseCurrency = 0m;
            MatchedTradesList = new List<TradeTaxCalculation>();
        }

        public void MatchTradeWithSection104(TradeTaxCalculation tradeTaxCalculation)
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
            MatchedTradesList.Add(tradeTaxCalculation);
        }

        private void AddAssets(TradeTaxCalculation tradeTaxCalculation)
        {
            if (tradeTaxCalculation.UnmatchedQty < 0)
            {
                throw new ArgumentOutOfRangeException
                    ($"Cannot add assets with negative quantity {tradeTaxCalculation.UnmatchedQty} and value {tradeTaxCalculation.UnmatchedNetAmount}");
            }
            (decimal qty, decimal value) = tradeTaxCalculation.MatchAll();
            tradeTaxCalculation.MatchHistory.Add(CreateUkMatchHistory(qty, value));
            Quantity += qty;
            ValueInBaseCurrency += value;
        }

        private void RemoveAssets(TradeTaxCalculation tradeTaxCalculation)
        {
            decimal qty;
            decimal value;
            if (Quantity == 0m) return;
            if (tradeTaxCalculation.UnmatchedQty <= Quantity)
            {
                (qty, _) = tradeTaxCalculation.MatchAll();

            }
            else
            {
                (qty, _) = tradeTaxCalculation.MatchQty(Quantity);
            }
            value = qty / Quantity * ValueInBaseCurrency;
            tradeTaxCalculation.MatchHistory.Add(CreateUkMatchHistory(qty, value));
            Quantity -= qty;
            ValueInBaseCurrency -= value;
        }

        private TradeMatch CreateUkMatchHistory(decimal qty, decimal value)
        {
            return new()
            {
                TradeMatchType = UkMatchType.SECTION_104,
                MatchedGroups = MatchedTradesList,
                MatchQuantity = qty,
                BaseCurrencyMatchValue = value
            };
        }
    }
}
