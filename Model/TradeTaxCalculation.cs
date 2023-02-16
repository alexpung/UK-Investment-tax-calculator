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

        public bool CalculationCompleted { get; private set; }

        public TradeTaxCalculation(IEnumerable<Trade> trades)
        {
            _tradeList = trades.ToList();
            TotaNetlAmount = trades.Sum(CalculateNetAmount);
            UnmatchedNetAmount = TotaNetlAmount;
            TotalQty = trades.Sum(trade => trade.Quantity);
            UnmatchedQty = TotalQty;
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
