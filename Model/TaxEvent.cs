using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodaMoney;

namespace CapitalGainCalculator.Model
{
    public abstract record TaxEvent
    {
        public string AssetName { get; }
        public DateTime Date { get; }

        protected TaxEvent(string assetName, DateTime date)
        {
           AssetName = assetName;
           Date = date;
        }
    }

    public record BuyTrade : TaxEvent
    {
        public decimal Quantity { get; }
        public DescribedMoney Proceed { get; }
        public IEnumerable<DescribedMoney>? Expenses { get; }
        public BuyTrade(string assetName, DateTime date, decimal quantity, DescribedMoney proceed, IEnumerable<DescribedMoney>? expenses = null) : base(assetName, date)
        {
            Quantity = quantity;
            Proceed = proceed;
            Expenses = expenses;
        }
    }

    public record SellTrade : TaxEvent
    {
        public decimal Quantity { get; }
        public DescribedMoney Proceed { get; }
        public IEnumerable<DescribedMoney>? Expenses { get; }
        public SellTrade(string assetName, DateTime date, decimal quantity, DescribedMoney proceed, IEnumerable<DescribedMoney>? expenses = null) : base(assetName, date)
        {
            Quantity = quantity;
            Proceed = proceed;
            Expenses = expenses;
        }
    }

    public record Dividend : TaxEvent
    {
        public RegionInfo CompanyLocation { get; }
        public DescribedMoney Proceed { get; }
        public ExchangeRate ExchangeRate { get; }
        public IEnumerable<DescribedMoney>? Expenses { get; }
        public Dividend(string assetName, DateTime date, RegionInfo companyLocation, DescribedMoney proceeds, ExchangeRate exchangeRate, IEnumerable<DescribedMoney>? expenses = null) : base(assetName, date)
        {
            Proceed = proceeds;
            Expenses = expenses;
            CompanyLocation = companyLocation;
            ExchangeRate = exchangeRate;
        }
    }

    public record StockSplit : TaxEvent
    {
        public ushort NumberBeforeSplit { get; }
        public ushort NumberAfterSplit { get; }
        public StockSplit(string assetName, DateTime date, ushort numberBeforeSplit, ushort numberAfterSplit) : base(assetName, date)
        {
            NumberBeforeSplit = numberBeforeSplit;
            NumberAfterSplit = numberAfterSplit;
        }
    }

    public record DescribedMoney
    {
        public string? Description { get; }
        public Money Amount { get; }

        public ExchangeRate ExchangeRate { get; }
        public DescribedMoney(Money amount, ExchangeRate exchangeRate, string? description = null)
        {
            Description = description;
            Amount = amount;
            ExchangeRate = exchangeRate;
        }
    }

}
