using Model.UkTaxModel.Stocks;

using TaxEvents;

namespace Model.UkTaxModel.Futures;

public class FutureTraceTaxCalculation : TradeTaxCalculation
{
    public FutureTraceTaxCalculation(IEnumerable<FutureContractTrade> trades) : base(trades)
    {
        TotalContractValue = trades.Sum(trade => trade.ContractValue.BaseCurrencyAmount);
        UnmatchedContractValue = TotalContractValue;
    }

    public decimal OpenQty { get; set; }
    public decimal CloseQty { get; set; }

    public WrappedMoney TotalContractValue { get; private set; }
    public WrappedMoney UnmatchedContractValue { get; private set; }
}
