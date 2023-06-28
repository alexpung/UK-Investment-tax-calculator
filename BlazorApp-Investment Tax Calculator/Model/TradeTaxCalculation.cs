using Enum;
using Model.Interfaces;
using NMoneys;

namespace Model;

public class TradeTaxCalculation : ITradeTaxCalculation
{
    public List<Trade> TradeList { get; init; }
    public List<TradeMatch> MatchHistory { get; init; } = new List<TradeMatch>();
    public Money TotalAllowableCost => MatchHistory.BaseCurrencySum(tradeMatch => tradeMatch.BaseCurrencyMatchAcquitionValue);
    public Money TotalProceeds => MatchHistory.BaseCurrencySum(tradeMatch => tradeMatch.BaseCurrencyMatchDisposalValue);
    public Money Gain => TotalProceeds - TotalAllowableCost;
    public Money TotalNetAmount { get; }
    private Money _unmatchedNetAmount;
    public Money UnmatchedNetAmount
    {
        get { return _unmatchedNetAmount; }
        private set
        {
            _unmatchedNetAmount = value;
            if (UnmatchedNetAmount.Amount == 0) CalculationCompleted = true;
        }
    }
    public decimal TotalQty { get; }
    public decimal UnmatchedQty { get; private set; }
    public TradeType BuySell { get; init; }
    public bool CalculationCompleted { get; private set; }
    public DateTime Date => TradeList[0].Date;
    public string AssetName => TradeList[0].AssetName;


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
        TotalNetAmount = trades.BaseCurrencySum(trade => trade.NetProceed);
        UnmatchedNetAmount = TotalNetAmount;
        TotalQty = trades.Sum(trade => trade.Quantity);
        UnmatchedQty = TotalQty;
        BuySell = trades.First().BuySell;
        CalculationCompleted = false;
    }

    public (decimal matchedQty, Money matchedValue) MatchQty(decimal demandedQty)
    {
        decimal matchedQty;
        Money matchedValue;
        if (demandedQty >= UnmatchedQty)
        {
            matchedQty = UnmatchedQty;
            matchedValue = UnmatchedNetAmount;
            UnmatchedQty = 0;
            UnmatchedNetAmount = BaseCurrencyMoney.BaseCurrencyZero;
        }
        else
        {
            matchedQty = demandedQty;
            matchedValue = TotalNetAmount.Multiply(demandedQty).Divide(TotalQty);
            UnmatchedQty -= matchedQty;
            UnmatchedNetAmount -= matchedValue;
        }
        return (matchedQty, matchedValue);
    }

    public (decimal matchedQty, Money matchedValue) MatchAll()
    {
        decimal matchedQty = UnmatchedQty;
        Money matchedValue = UnmatchedNetAmount;
        UnmatchedQty = 0;
        UnmatchedNetAmount = BaseCurrencyMoney.BaseCurrencyZero;
        return (matchedQty, matchedValue);
    }
}
