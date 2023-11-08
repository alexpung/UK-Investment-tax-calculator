namespace Model.UkTaxModel;

public record UkSection104
{
    public string AssetName { get; init; }
    public decimal Quantity { get; set; }
    public WrappedMoney AcquisitionCostInBaseCurrency { get; set; }
    public WrappedMoney TotalContractValue { get; private set; } // Contract value that determine profit and loss but not actually money paid or received e.g. future cotract price
    public List<Section104History> Section104HistoryList { get; set; } = new();

    public UkSection104(string name)
    {
        AssetName = name;
        Quantity = 0m;
        AcquisitionCostInBaseCurrency = WrappedMoney.GetBaseCurrencyZero();
        TotalContractValue = WrappedMoney.GetBaseCurrencyZero();
    }

    public void AdjustValues(decimal quantity, WrappedMoney acquisitionCostInBaseCurrency, WrappedMoney? contractValue = null)
    {
        Quantity += quantity;
        AcquisitionCostInBaseCurrency += acquisitionCostInBaseCurrency;
        if (contractValue != null) TotalContractValue += contractValue;
    }
}
