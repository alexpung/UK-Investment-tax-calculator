﻿using Enum;
using Model.Interfaces;
using Model.UkTaxModel;

namespace Model.TaxEvents;

public record StockSplit : CorporateAction, IChangeSection104, IChangeTradeMatchingInBetween
{
    public required int NumberBeforeSplit { get; set; }
    public required int NumberAfterSplit { get; set; }
    public bool Rounding { get; set; } = true;

    public void ChangeTradeMatching(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, TradeMatch tradeMatch)
    {
        ITradeTaxCalculation earlierTrade = trade1.Date <= trade2.Date ? trade1 : trade2;
        ITradeTaxCalculation laterTrade = trade1.Date > trade2.Date ? trade1 : trade2;
        if ((earlierTrade.Date < Date) && (Date < laterTrade.Date))
        {
            if (earlierTrade.BuySell == TradeType.BUY)
            {
                tradeMatch.MatchAcquisitionQty = tradeMatch.MatchAcquisitionQty * NumberBeforeSplit / NumberAfterSplit;
                tradeMatch.BaseCurrencyMatchAcquisitionValue = earlierTrade.GetNetAmount(tradeMatch.MatchAcquisitionQty);
            }
            else
            {
                tradeMatch.MatchDisposalQty = tradeMatch.MatchDisposalQty * NumberBeforeSplit / NumberAfterSplit;
                tradeMatch.BaseCurrencyMatchDisposalValue = earlierTrade.GetNetAmount(tradeMatch.MatchDisposalQty);
            }
            tradeMatch.AdditionalInformation += $"Stock split occurred at {Date.Date} with ratio of {NumberAfterSplit} for {NumberBeforeSplit}\n";
        }
    }

    public void ChangeSection104(UkSection104 section104)
    {
        decimal oldQuantity = section104.Quantity;
        decimal newQuantity = GetSharesAfterSplit(oldQuantity);
        section104.Section104HistoryList.Add(Section104History.ShareAdjustment(Date, oldQuantity, newQuantity));
        section104.Quantity = newQuantity;
    }

    public decimal GetSharesAfterSplit(decimal quantity)
    {
        decimal result = quantity * NumberAfterSplit / NumberBeforeSplit;
        return Rounding ? Math.Round(result, MidpointRounding.ToZero) : result;
    }
}