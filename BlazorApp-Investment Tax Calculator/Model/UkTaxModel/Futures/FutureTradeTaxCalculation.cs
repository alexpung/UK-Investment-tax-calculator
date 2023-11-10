using Enum;

using Model.UkTaxModel.Stocks;

using TaxEvents;

namespace Model.UkTaxModel.Futures;

public class FutureTradeTaxCalculation : TradeTaxCalculation
{
    public override TradeType BuySell => PositionType is FuturePositionType.OPENLONG or FuturePositionType.OPENSHORT ? TradeType.BUY : TradeType.SELL;
    public FuturePositionType PositionType => ((FutureContractTrade)TradeList[0]).FuturePositionType;
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
        if (PositionType is FuturePositionType.OPENSHORT or FuturePositionType.CLOSELONG)
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
        if (BuySell is TradeType.BUY)
        {
            Section104History section104History = ukSection104.AddAssets(this, UnmatchedQty, UnmatchedCostOrProceed, UnmatchedContractValue);
            MatchHistory.Add(TradeMatch.CreateSection104Match(UnmatchedQty, UnmatchedCostOrProceed, WrappedMoney.GetBaseCurrencyZero(), section104History));
            MatchQty(UnmatchedQty);
        }
        else if (BuySell is TradeType.SELL)
        {
            if (ukSection104.Quantity == 0m) return;
            decimal matchQty = Math.Min(UnmatchedQty, ukSection104.Quantity);
            Section104History section104History = ukSection104.RemoveAssets(this, UnmatchedQty);
            WrappedMoney contractGain = GetProportionedContractValue(matchQty) + section104History.ContractValueChange;
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
            MatchHistory.Add(TradeMatch.CreateSection104Match(matchQty, acquisitionValue, disposalValue, section104History));
            MatchQty(matchQty);
        }
    }
}
