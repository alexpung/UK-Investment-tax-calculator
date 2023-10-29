using TaxEvents;

namespace Model.UkTaxModel;

public class FutureTraceTaxCalculation : TradeTaxCalculation
{
    public FutureTraceTaxCalculation(IEnumerable<FutureContractTrade> trades) : base(trades)
    {
        TotalContractValue = trades.Sum(trade => trade.ContractValue.BaseCurrencyAmount);
        UnmatchedContractValue = TotalContractValue;
    }

    public WrappedMoney TotalContractValue { get; private set; }
    public WrappedMoney UnmatchedContractValue { get; private set; }
}
