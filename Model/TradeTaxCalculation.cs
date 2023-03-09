using CapitalGainCalculator.Enum;
using CapitalGainCalculator.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CapitalGainCalculator.Model;

public class TradeTaxCalculation : ITradeTaxCalculation
{
    public List<Trade> TradeList { get; init; }
    public List<TradeMatch> MatchHistory { get; init; } = new List<TradeMatch>();
    public decimal TotalNetAmount { get; }
    private decimal _unmatchedNetAmount;
    public decimal UnmatchedNetAmount
    {
        get { return _unmatchedNetAmount; }
        private set
        {
            _unmatchedNetAmount = value;
            if (UnmatchedNetAmount == 0) CalculationCompleted = true;
        }
    }
    public decimal TotalQty { get; }
    public decimal UnmatchedQty { get; private set; }
    public TradeType BuySell { get; init; }
    public bool CalculationCompleted { get; private set; }

    /// <summary>
    /// Bunch a group of trade on the same side so that they can be matched together as a group, e.g. UK tax trades on the same side on the same day and same capacity are grouped.
    /// </summary>
    /// <param name="trades">Only accept trade from the same side</param>
    public TradeTaxCalculation(IEnumerable<Trade> trades)
    {
        if (!trades.All(i => i.BuySell.Equals(trades.First().BuySell)))
        {
            throw new ArgumentException("Not all trades that is put in TradeTaxCalculation is on the same BUY/SELL side");
        }
        TradeList = trades.ToList();
        TotalNetAmount = trades.Sum(CalculateNetAmount);
        UnmatchedNetAmount = TotalNetAmount;
        TotalQty = trades.Sum(trade => trade.Quantity);
        UnmatchedQty = TotalQty;
        BuySell = trades.First().BuySell;
        CalculationCompleted = false;
    }

    public (decimal matchedQty, decimal matchedValue) MatchQty(decimal demandedQty)
    {
        decimal matchedQty;
        decimal matchedValue;
        if (demandedQty >= UnmatchedQty)
        {
            matchedQty = UnmatchedQty;
            matchedValue = UnmatchedNetAmount;
            UnmatchedQty = 0;
            UnmatchedNetAmount = 0;
        }
        else
        {
            matchedQty = demandedQty;
            matchedValue = TotalNetAmount * demandedQty / TotalQty;
            UnmatchedQty -= matchedQty;
            UnmatchedNetAmount -= matchedValue;
        }
        return (matchedQty, matchedValue);
    }

    public (decimal matchedQty, decimal matchedValue) MatchAll()
    {
        decimal matchedQty = UnmatchedQty;
        decimal matchedValue = UnmatchedNetAmount;
        UnmatchedQty = 0;
        UnmatchedNetAmount = 0;
        return (matchedQty, matchedValue);
    }

    private decimal CalculateNetAmount(Trade trade)
    {
        decimal deductiable;
        if (trade.Expenses.Any())
        {
            deductiable = trade.Expenses.Sum(expense => expense.BaseCurrencyAmount);
        }
        else deductiable = 0;
        return trade.Proceed.BaseCurrencyAmount - deductiable;
    }
}
