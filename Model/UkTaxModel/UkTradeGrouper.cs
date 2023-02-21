using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapitalGainCalculator.Model.UkTaxModel
{
    public class UkTradeGrouper
    {
        public static List<TradeTaxCalculation> GroupTrade(IEnumerable<TaxEvent> taxEvents)
        {
            IEnumerable<Trade> trades = from taxEvent in taxEvents
                                              where taxEvent is Trade
                                              select (Trade)taxEvent;

            var groupedTrade = from trade in trades
                               group trade by new { trade.AssetName, trade.Date, trade.BuySell };
            return groupedTrade.Select(group => new TradeTaxCalculation(group)).ToList();
        }
    }
}
