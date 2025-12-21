using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Futures;
using InvestmentTaxCalculator.Model.UkTaxModel.Fx;
using InvestmentTaxCalculator.Model.UkTaxModel.Options;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using Microsoft.AspNetCore.Http;

namespace InvestmentTaxCalculator.Model;

public class TradeTaxCalculationFactory(ResidencyStatusRecord residencyStatusRecord)
{
    /// <summary>
    /// A trade can be a opening a position, closing a position or closing and reopen a position in opposite direction.
    /// For each trade calculate how much of the trade is opening and closing a position.
    /// </summary>
    /// <param name="trades"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public List<FutureTradeTaxCalculation> GroupFutureTrade(IEnumerable<FutureContractTrade> trades)
    {
        List<FutureTradeTaxCalculation> groupedTrade = [];
        foreach (var tradeGroup in GroupFutureContractTradeByAssetName(trades))
        {
            List<FutureContractTrade> taggedTrades = UkMatchingRules.TagTradesWithOpenClose(tradeGroup);
            // trade.AssetName grouping is required as short positions is treated as a separate asset
            groupedTrade.AddRange(taggedTrades.GroupBy(trade => (trade.Date.Date, trade.AcquisitionDisposal, trade.AssetName)).Select(trades => new FutureTradeTaxCalculation(trades)));
        }
        SetResidencyStatus(groupedTrade, residencyStatusRecord);
        return groupedTrade;
    }

    public List<TradeTaxCalculation> GroupTrade(IEnumerable<Trade> trades)
    {
        var groupedTrade = from trade in trades
                           where trade.AssetType == AssetCategoryType.STOCK
                           group trade by new { trade.AssetName, trade.Date.Date, trade.AcquisitionDisposal };
        var groupedFxTrade = from trade in trades
                             where trade.AssetType == AssetCategoryType.FX
                             group trade by new { trade.AssetName, trade.Date.Date, trade.AcquisitionDisposal };
        IEnumerable<TradeTaxCalculation> groupedTradeCalculations = groupedTrade.Select(group => new TradeTaxCalculation(group));
        IEnumerable<TradeTaxCalculation> groupedFxTradeCalculations = groupedFxTrade.Select(group => new FxTradeTaxCalculation(group));
        var groupedList = groupedTradeCalculations.Concat(groupedFxTradeCalculations).ToList();
        SetResidencyStatus(groupedList, residencyStatusRecord);
        return groupedList;
    }

    public List<OptionTradeTaxCalculation> GroupOptionTrade(IEnumerable<OptionTrade> trades)
    {
        var groupedTrade = from trade in trades
                           group trade by new { trade.AssetName, trade.Date.Date, trade.AcquisitionDisposal };
        var groupedList = groupedTrade.Select(group => new OptionTradeTaxCalculation(group)).ToList();
        SetResidencyStatus(groupedList, residencyStatusRecord);
        return groupedList;
    }

    private static IEnumerable<IGrouping<string, FutureContractTrade>> GroupFutureContractTradeByAssetName(IEnumerable<FutureContractTrade> trades)
    {
        return from trade in trades
               group trade by trade.AssetName;
    }

    private static void SetResidencyStatus(IEnumerable<ITradeTaxCalculation> tradeCalculations, ResidencyStatusRecord residencyStatusRecord)
    {
        foreach (var trade in tradeCalculations)
        {
            trade.ResidencyStatusAtTrade = residencyStatusRecord.GetResidencyStatus(DateOnly.FromDateTime(trade.Date));
        }
    }
}
