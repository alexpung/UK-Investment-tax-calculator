﻿using CapitalGainCalculator.Enum;
using NodaMoney;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace CapitalGainCalculator.Model;

public abstract record TaxEvent
{
    public string AssetName { get; set; } = "";
    public DateTime Date { get; set; }
}

public record Trade : TaxEvent
{
    public TradeType BuySell { get; set; }
    public decimal Quantity { get; set; }
    public DescribedMoney Proceed { get; set; } = new DescribedMoney();
    public List<DescribedMoney> Expenses { get; set; } = new List<DescribedMoney>();
}

public record Dividend : TaxEvent
{
    public DividendType DividendType { get; set; }
    public RegionInfo CompanyLocation { get; set; } = RegionInfo.CurrentRegion;
    public DescribedMoney Proceed { get; set; } = new DescribedMoney();
}

public abstract record CorporateAction : TaxEvent
{
}

public record StockSplit : CorporateAction
{
    public int NumberBeforeSplit { get; set; }
    public int NumberAfterSplit { get; set; }
    public bool Rounding { get; set; } = true;

    public decimal GetSharesAfterSplit(decimal quantity)
    {
        decimal result = quantity * NumberAfterSplit / NumberBeforeSplit;
        return Rounding ? Math.Round(result, MidpointRounding.ToZero) : result;
    }


}

public record DescribedMoney
{
    public string Description { get; set; } = "";
    public Money Amount { get; set; }

    public decimal FxRate { get; set; }

    public decimal BaseCurrencyAmount => Amount.Amount * FxRate;
}
