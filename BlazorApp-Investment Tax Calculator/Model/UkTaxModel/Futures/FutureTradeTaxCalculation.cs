﻿using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using System.Text;

namespace InvestmentTaxCalculator.Model.UkTaxModel.Futures;

public class FutureTradeTaxCalculation : TradeTaxCalculation
{
    public override TradeType AcquisitionDisposal => PositionType is PositionType.OPENLONG or PositionType.OPENSHORT ? TradeType.ACQUISITION : TradeType.DISPOSAL;
    public PositionType PositionType => ((FutureContractTrade)TradeList[0]).PositionType;
    public WrappedMoney TotalContractValue { get; private set; }
    public decimal ContractFxRate { get; private init; }
    public WrappedMoney UnmatchedContractValue { get; private set; }
    public WrappedMoney GetProportionedContractValue(decimal qty) => TotalContractValue * qty / TotalQty;
    public FutureTradeTaxCalculation(IEnumerable<FutureContractTrade> trades) : base(trades)
    {
        TotalContractValue = trades.Sum(trade => trade.ContractValue.Amount);
        ContractFxRate = trades.First().ContractValue.FxRate;
        UnmatchedContractValue = TotalContractValue;
        // This special case require modification as Future contract start from 0 cost
        // normally commission are deducted from money received in a sell trade
        // In case of open short is a buy trade and TotalCostOrProceed is cost of getting the contract commissions are added instead
        // The opposite is true for CLOSELONG
        if (PositionType is PositionType.OPENSHORT or PositionType.CLOSELONG)
        {
            TotalCostOrProceed *= -1;
            UnmatchedCostOrProceed *= -1;
        }
    }

    public override void MatchQty(decimal demandedQty)
    {
        base.MatchQty(demandedQty);
        UnmatchedContractValue -= TotalContractValue * demandedQty / TotalQty;
    }

    public override void MatchWithSection104(UkSection104 ukSection104)
    {
        if (CalculationCompleted) return;
        if (AcquisitionDisposal is TradeType.ACQUISITION)
        {
            Section104History section104History = ukSection104.AddAssets(this, UnmatchedQty, UnmatchedCostOrProceed, UnmatchedContractValue);
            FutureTradeMatch tradeMatch = new()
            {
                Date = DateOnly.FromDateTime(Date),
                AssetName = AssetName,
                TradeMatchType = TaxMatchType.SECTION_104,
                MatchAcquisitionQty = UnmatchedQty,
                MatchDisposalQty = 0,
                BaseCurrencyMatchAllowableCost = UnmatchedCostOrProceed,
                BaseCurrencyMatchDisposalProceed = WrappedMoney.GetBaseCurrencyZero(),
                MatchedBuyTrade = this,
                MatchedSellTrade = null,
                AdditionalInformation = "",
                MatchBuyContractValue = UnmatchedContractValue,
                BaseCurrencyAcquisitionDealingCost = UnmatchedCostOrProceed,
                BaseCurrencyDisposalDealingCost = WrappedMoney.GetBaseCurrencyZero(),
                ClosingFxRate = 0,
                Section104HistorySnapshot = section104History,
            };
            MatchHistory.Add(tradeMatch);
            MatchQty(UnmatchedQty);
        }
        else if (AcquisitionDisposal is TradeType.DISPOSAL)
        {
            if (ukSection104.Quantity == 0m) return;
            decimal matchQty = Math.Min(UnmatchedQty, ukSection104.Quantity);
            Section104History section104History = ukSection104.RemoveAssets(this, UnmatchedQty);

            WrappedMoney buyContractValue = PositionType switch
            {
                PositionType.CLOSELONG => section104History.ContractValueChange * -1,
                PositionType.CLOSESHORT => GetProportionedContractValue(matchQty),
                _ => throw new ArgumentException($"Unexpected future position type {PositionType} for close position")
            };
            WrappedMoney sellContractValue = PositionType switch
            {
                PositionType.CLOSELONG => GetProportionedContractValue(matchQty),
                PositionType.CLOSESHORT => section104History.ContractValueChange * -1,
                _ => throw new ArgumentException($"Unexpected future position type {PositionType} for close position")
            };
            WrappedMoney contractGain = sellContractValue - buyContractValue;
            WrappedMoney contractGainInBaseCurrency = new((contractGain * ContractFxRate).Amount);
            WrappedMoney acquisitionValue = (section104History.ValueChange * -1) + GetProportionedCostOrProceed(matchQty);
            WrappedMoney disposalValue = WrappedMoney.GetBaseCurrencyZero();
            if (contractGainInBaseCurrency.Amount > 0)
            {
                disposalValue += contractGainInBaseCurrency;
            }
            else
            {
                acquisitionValue += contractGainInBaseCurrency * -1;
            }
            FutureTradeMatch tradeMatch = new()
            {
                Date = DateOnly.FromDateTime(Date),
                AssetName = AssetName,
                TradeMatchType = TaxMatchType.SECTION_104,
                MatchAcquisitionQty = matchQty,
                MatchDisposalQty = matchQty,
                BaseCurrencyMatchAllowableCost = acquisitionValue,
                BaseCurrencyMatchDisposalProceed = disposalValue,
                MatchedBuyTrade = null,
                MatchedSellTrade = this,
                AdditionalInformation = "",
                MatchBuyContractValue = buyContractValue,
                MatchSellContractValue = sellContractValue,
                BaseCurrencyAcquisitionDealingCost = section104History.ValueChange * -1,
                BaseCurrencyDisposalDealingCost = GetProportionedCostOrProceed(matchQty),
                ClosingFxRate = ContractFxRate,
                Section104HistorySnapshot = section104History,
            };
            MatchHistory.Add(tradeMatch);
            MatchQty(matchQty);
        }
    }

    public override string PrintToTextFile()
    {
        StringBuilder output = new();
        output.Append($"{PositionType.GetDescription()} {TotalQty} units of {AssetName} on " +
            $"{Date:d}.\t");
        output.AppendLine($"Total gain (loss): {Gain}");
        output.AppendLine($"Trade details:");
        foreach (var trade in TradeList)
        {
            output.AppendLine($"\t{trade.PrintToTextFile()}");
        }
        output.AppendLine($"Trade matching:");
        foreach (var matching in MatchHistory)
        {
            output.AppendLine(matching.PrintToTextFile());
        }
        if (MatchHistory.Count > 2)
        {
            output.AppendLine($"Resulting overall gain for this disposal: {GetSumFormula(MatchHistory.Select(match => match.MatchGain))}");
        }
        output.AppendLine();
        return output.ToString();
    }
}
