using Enum;

using Model.UkTaxModel.Stocks;

using TaxEvents;

namespace Model.UkTaxModel.Futures;

public class FutureTradeTaxCalculation : TradeTaxCalculation
{
    public override TradeType BuySell => PositionType is FuturePositionType.OPENLONG or FuturePositionType.OPENSHORT ? TradeType.BUY : TradeType.SELL;
    public FutureTradeTaxCalculation(IEnumerable<FutureContractTrade> trades) : base(trades)
    {
        TotalContractValue = trades.Sum(trade => trade.ContractValue.BaseCurrencyAmount);
        UnmatchedContractValue = TotalContractValue;
    }

    public FuturePositionType PositionType => ((FutureContractTrade)TradeList[0]).FuturePositionType;
    public WrappedMoney TotalContractValue { get; private set; }
    public WrappedMoney UnmatchedContractValue { get; private set; }
    public WrappedMoney GetProportionedContractValue(decimal qty) => TotalContractValue * qty / TotalQty;
}
