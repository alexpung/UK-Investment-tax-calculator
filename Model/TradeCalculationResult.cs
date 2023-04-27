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

    // Rounding to tax payer benefit https://www.gov.uk/hmrc-internal-manuals/self-assessment-manual/sam121370
    public int NumberOfDisposals(Func<TradeTaxCalculation, bool> filterCondition) => CalculatedTrade.Where(filterCondition).Count(trade => trade.BuySell == Enum.TradeType.SELL);
    public int DisposalProceeds(Func<TradeTaxCalculation, bool> filterCondition) => (int)Math.Floor(CalculatedTrade.Where(filterCondition)
        .Where(trade => trade.BuySell == Enum.TradeType.SELL).Sum(trade => trade.TotalProceeds));
    public int AllowableCosts(Func<TradeTaxCalculation, bool> filterCondition) => (int)Math.Ceiling(CalculatedTrade.Where(filterCondition)
        .Where(trade => trade.BuySell == Enum.TradeType.SELL).Sum(trade => trade.TotalAllowableCost));
    public int TotalGain(Func<TradeTaxCalculation, bool> filterCondition) => (int)Math.Floor(CalculatedTrade.Where(filterCondition)
        .Where(trade => trade.BuySell == Enum.TradeType.SELL).Where(trade => trade.Gain > 0).Sum(trade => trade.Gain));
    public int TotalLoss(Func<TradeTaxCalculation, bool> filterCondition) => (int)Math.Ceiling(CalculatedTrade.Where(filterCondition)
        .Where(trade => trade.BuySell == Enum.TradeType.SELL).Where(trade => trade.Gain < 0).Sum(trade => trade.Gain));
}
