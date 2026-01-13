using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;

using static InvestmentTaxCalculator.Model.ResidencyStatusRecord;

namespace InvestmentTaxCalculator.Model.UkTaxModel;

public record UkSection104
{
    public string AssetName { get; init; }
    public decimal Quantity { get; set; }
    public WrappedMoney AcquisitionCostInBaseCurrency { get; set; }
    public WrappedMoney TotalContractValue { get; private set; } // Contract value that determine profit and loss but not actually money paid or received e.g. future cotract price
    public List<Section104History> Section104HistoryList { get; set; } = [];
    // Quantity of assets acquired while non-resident for tax purposes, used for calculating gain exempt from UK tax when returning to UK residency
    public Dictionary<RangeEntry, decimal> AcquiredQuantityByResidencyRange { get; set; } = new();
    public ResidencyStatusRecord? ResidencyStatusRecord { get; init; } = null;

    public UkSection104(string name)
    {
        AssetName = name;
        Quantity = 0m;
        AcquisitionCostInBaseCurrency = WrappedMoney.GetBaseCurrencyZero();
        TotalContractValue = WrappedMoney.GetBaseCurrencyZero();
    }

    public UkSection104(string name, ResidencyStatusRecord residencyStatusRecord) : this(name)
    {
        ResidencyStatusRecord = residencyStatusRecord;
    }

    private void AdjustValues(decimal quantity, WrappedMoney acquisitionCostInBaseCurrency, WrappedMoney? contractValue = null)
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
        if (ResidencyStatusRecord is not null)
        {
            RangeEntry? currentRange = ResidencyStatusRecord.GetResidencyStatusRange(DateOnly.FromDateTime(tradeTaxCalculation.Date));
            if (currentRange is not null)
            {
                decimal newQuantity = AcquiredQuantityByResidencyRange.TryGetValue(currentRange, out decimal existingQuantity) ? existingQuantity + addedQuantity : addedQuantity;
                AcquiredQuantityByResidencyRange[currentRange] = newQuantity;
            }
        }
        return newSection104History;
    }

    public List<Section104MatchResults> RemoveAssets(ITradeTaxCalculation tradeTaxCalculation, decimal removedQuantity)
    {
        Section104History section104History;
        if (ResidencyStatusRecord is null)
        {
            section104History = RemoveAssetsPrivate(removedQuantity, tradeTaxCalculation);
            return [new Section104MatchResults(removedQuantity, section104History, TaxableStatus.TAXABLE)];
        }
        switch (ResidencyStatusRecord.GetResidencyStatus(DateOnly.FromDateTime(tradeTaxCalculation.Date)))
        {
            case ResidencyStatus.NonResident:
                section104History = RemoveAssetsPrivate(removedQuantity, tradeTaxCalculation);
                return [new Section104MatchResults(removedQuantity, section104History, TaxableStatus.NON_TAXABLE)];
            case ResidencyStatus.Resident:
                section104History = RemoveAssetsPrivate(removedQuantity, tradeTaxCalculation);
                return [new Section104MatchResults(removedQuantity, section104History, TaxableStatus.TAXABLE)];
            case ResidencyStatus.TemporaryNonResident:
                List<Section104MatchResults> results = [];
                RangeEntry residencyRange = ResidencyStatusRecord.GetResidencyStatusRange(DateOnly.FromDateTime(tradeTaxCalculation.Date))!;
                decimal nonResidentQuantityAvailable = AcquiredQuantityByResidencyRange.TryGetValue(residencyRange, out decimal existingQuantity) ? existingQuantity : 0m;
                // There is more than enough quantity acquired in the same TNR period to cover the removed quantity
                if (nonResidentQuantityAvailable >= removedQuantity)
                {
                    section104History = RemoveAssetsPrivate(removedQuantity, tradeTaxCalculation);
                    section104History.Explanation = $"{removedQuantity} unit(s) removed from the same temporary non-resident period and are not taxable";
                    AcquiredQuantityByResidencyRange[residencyRange] = nonResidentQuantityAvailable - removedQuantity;
                    results.Add(new Section104MatchResults(removedQuantity, section104History, TaxableStatus.NON_TAXABLE));
                    return results;
                }
                // There is not enough quantity acquired in the same TNR period to cover the removed quantity
                else
                {
                    if (nonResidentQuantityAvailable > 0)
                    {
                        section104History = RemoveAssetsPrivate(nonResidentQuantityAvailable, tradeTaxCalculation);
                        section104History.Explanation = $"{nonResidentQuantityAvailable} unit(s) removed from the same temporary non-resident period and are not taxable";
                        AcquiredQuantityByResidencyRange[residencyRange] = 0m;
                        results.Add(new Section104MatchResults(nonResidentQuantityAvailable, section104History, TaxableStatus.NON_TAXABLE));
                    }
                    decimal remainingQuantity = removedQuantity - nonResidentQuantityAvailable;
                    section104History = RemoveAssetsPrivate(remainingQuantity, tradeTaxCalculation);
                    results.Add(new Section104MatchResults(remainingQuantity, section104History, TaxableStatus.TAXABLE));
                    return results;
                }
            default:
                {
                    throw new InvalidOperationException($"Unknown residency status for asset {AssetName} on date {tradeTaxCalculation.Date}");
                }
        }
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

    /// <summary>
    /// Used in corporate actions like stock split to multiply the quantity in the S104 pool
    /// </summary>
    /// <param name="factor"></param>
    public void MultiplyQuantity(decimal factor, DateTime date, string explanation)
    {
        decimal newQuantity = factor * Quantity;
        Section104HistoryList.Add(Section104History.ShareAdjustment(date, Quantity, newQuantity, AcquisitionCostInBaseCurrency, explanation, TotalContractValue));
        Quantity = newQuantity;
        AcquiredQuantityByResidencyRange = AcquiredQuantityByResidencyRange.ToDictionary(kvp => kvp.Key, kvp => kvp.Value * factor);
    }

    public void AdjustAcquisitionCost(WrappedMoney adjustmentAmount, DateTime date, string explanation, WrappedMoney? adjustContractValue = null)
    {
        Section104HistoryList.Add(Section104History.ValueAdjustment(date, Quantity, AcquisitionCostInBaseCurrency, adjustmentAmount, explanation, TotalContractValue, adjustContractValue));
        AcquisitionCostInBaseCurrency += adjustmentAmount;
        if (adjustContractValue is not null)
        {
            TotalContractValue += adjustContractValue;
        }
    }

    private Section104History RemoveAssetsPrivate(decimal removedQuantity, ITradeTaxCalculation tradeTaxCalculation)
    {
        if (removedQuantity > Quantity) throw new ArgumentException($"Invalid remove assets: {AssetName}, removed quantity {removedQuantity} when quantity is {Quantity}");
        WrappedMoney costAdjustment = AcquisitionCostInBaseCurrency * removedQuantity * -1 / Quantity;
        WrappedMoney contractValueAdjustment = TotalContractValue * removedQuantity * -1 / Quantity;
        decimal quantityAdjustment = removedQuantity * -1;
        Section104History newSection104History = Section104History.AdjustSection104(tradeTaxCalculation, quantityAdjustment, costAdjustment, Quantity,
                AcquisitionCostInBaseCurrency, TotalContractValue, contractValueAdjustment);
        Section104HistoryList.Add(newSection104History);
        AdjustValues(quantityAdjustment, costAdjustment, contractValueAdjustment);
        return newSection104History;
    }
}

public record Section104MatchResults(decimal QuantityMatched, Section104History Section104HistoryResult, TaxableStatus IsTaxable);
