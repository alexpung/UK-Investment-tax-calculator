using System;
using System.Collections.Generic;
using System.Linq;

namespace CapitalGainCalculator.Model;

public class TradeCalculationResult
{
    public List<TradeTaxCalculation> CalculatedTrade { get; set; } = new();

    public void SetResult(List<TradeTaxCalculation> tradeTaxCalculations)
    {
        CalculatedTrade = tradeTaxCalculations;
    }

    public int NumberOfDisposals(Func<TradeTaxCalculation, bool> filterCondition) => CalculatedTrade.Where(filterCondition).Count(trade => trade.BuySell == Enum.TradeType.SELL);
    public decimal DisposalProceeds(Func<TradeTaxCalculation, bool> filterCondition) => CalculatedTrade.Where(filterCondition).Sum(trade => trade.TotalProceeds);
    public decimal AllowableCosts(Func<TradeTaxCalculation, bool> filterCondition) => CalculatedTrade.Where(filterCondition).Sum(trade => trade.TotalAllowableCost);
    public decimal TotalGain(Func<TradeTaxCalculation, bool> filterCondition) => CalculatedTrade.Where(filterCondition).Where(trade => trade.Gain > 0).Sum(trade => trade.Gain);
    public decimal TotalLoss(Func<TradeTaxCalculation, bool> filterCondition) => CalculatedTrade.Where(filterCondition).Where(trade => trade.Gain < 0).Sum(trade => trade.Gain);
}
