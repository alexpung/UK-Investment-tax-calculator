using CapitalGainCalculator.Enum;
using System.Collections.Generic;

namespace CapitalGainCalculator.Model.Interfaces;
public interface ITradeTaxCalculation
{
    TradeType BuySell { get; init; }
    bool CalculationCompleted { get; }
    List<TradeMatch> MatchHistory { get; init; }
    decimal TotalNetAmount { get; }
    decimal TotalQty { get; }
    List<Trade> TradeList { get; init; }
    decimal UnmatchedNetAmount { get; }
    decimal UnmatchedQty { get; }

    (decimal matchedQty, decimal matchedValue) MatchAll();
    (decimal matchedQty, decimal matchedValue) MatchQty(decimal demandedQty);
}