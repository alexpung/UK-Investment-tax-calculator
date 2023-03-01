using CapitalGainCalculator.Enum;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapitalGainCalculator.Model
{
    public class TradeTaxCalculation
    {
        private readonly List<Trade> _tradeList;
        public decimal TotaNetlAmount { get; }
        private decimal _unmatchedNetAmount;
        public decimal UnmatchedNetAmount
        {
            get { return _unmatchedNetAmount; }
            set
            {
                _unmatchedNetAmount = value;
                if(UnmatchedNetAmount == 0) CalculationCompleted = true;
            }
        }
        public decimal TotalQty { get; }
        public decimal UnmatchedQty { get; set; }
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
            _tradeList = trades.ToList();
            TotaNetlAmount = trades.Sum(CalculateNetAmount);
            UnmatchedNetAmount = TotaNetlAmount;
            TotalQty = trades.Sum(trade => trade.Quantity);
            UnmatchedQty = TotalQty;
            BuySell = trades.First().BuySell;
            CalculationCompleted = false;
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
}
