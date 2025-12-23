using InvestmentTaxCalculator.Model.Interfaces;

namespace InvestmentTaxCalculator.Model.UkTaxModel;

public record UkSection104
{
    public string AssetName { get; init; }
    public decimal Quantity { get; set; }
    public WrappedMoney AcquisitionCostInBaseCurrency { get; set; }
    public WrappedMoney TotalContractValue { get; private set; } // Contract value that determine profit and loss but not actually money paid or received e.g. future cotract price
    public List<Section104History> Section104HistoryList { get; set; } = [];

    public UkSection104(string name)
    {
        AssetName = name;
        Quantity = 0m;
        AcquisitionCostInBaseCurrency = WrappedMoney.GetBaseCurrencyZero();
        TotalContractValue = WrappedMoney.GetBaseCurrencyZero();
    }

    private void AdjustValues(decimal quantity, WrappedMoney acquisitionCostInBaseCurrency, WrappedMoney? contractValue = null, decimal nonResidentExemptQuantity = 0)
    {
        Quantity += quantity;
        AcquisitionCostInBaseCurrency += acquisitionCostInBaseCurrency;
        if (TotalContractValue == WrappedMoney.GetBaseCurrencyZero() && contractValue != null)
        {
            TotalContractValue = contractValue;
        }
        else if (contractValue != null)
        {
            TotalContractValue += contractValue;
        }
    }

    public Section104History AddAssets(ITradeTaxCalculation tradeTaxCalculation, decimal addedQuantity, WrappedMoney addedAcquisitionCostInBaseCurrency,
                                       WrappedMoney? addedContractValue = null)
    {
        Section104History newSection104History = Section104History.AdjustSection104(tradeTaxCalculation, addedQuantity, addedAcquisitionCostInBaseCurrency, Quantity,
                AcquisitionCostInBaseCurrency, TotalContractValue, addedContractValue);
        Section104HistoryList.Add(newSection104History);
        AdjustValues(addedQuantity, addedAcquisitionCostInBaseCurrency, addedContractValue);
        return newSection104History;
    }

    public Section104History RemoveAssets(ITradeTaxCalculation tradeTaxCalculation, decimal removedQuantity)
    {
        if (removedQuantity < 0) throw new ArgumentException($"Invalid remove quantity for {tradeTaxCalculation.AssetName} on {tradeTaxCalculation.Date.ToShortDateString()}" +
            $"from S104 {removedQuantity}, must be greater than zero.");
        WrappedMoney costAdjustment = AcquisitionCostInBaseCurrency * removedQuantity * -1 / Quantity;
        WrappedMoney contractValueAdjustment = TotalContractValue * removedQuantity * -1 / Quantity;
        decimal quantityAdjustment = removedQuantity * -1;
        Section104History newSection104History = Section104History.AdjustSection104(tradeTaxCalculation, quantityAdjustment, costAdjustment, Quantity,
                AcquisitionCostInBaseCurrency, TotalContractValue, contractValueAdjustment);
        Section104HistoryList.Add(newSection104History);
        AdjustValues(quantityAdjustment, costAdjustment, contractValueAdjustment);
        return newSection104History;
    }

    /// <summary>
    /// Get the last Section104History before the date
    /// Return null if no history before the date
    /// </summary>
    /// <param name="date">The date of which you want to get the state of the S104 pool</param>
    /// <returns></returns>
    public Section104History? GetLastSection104History(DateOnly date)
    {
        return Section104HistoryList.LastOrDefault(x => DateOnly.FromDateTime(x.Date) <= date);
    }
}
