using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.UkTaxModel;

namespace InvestmentTaxCalculator.ViewModel;

public class Section104HistoryViewModel(Section104History section104History, string assetName)
{
    public string AssetName { get; } = assetName;
    public int? TradeId { get; } = section104History.TradeTaxCalculation?.Id;
    public DateTime Date { get; } = section104History.Date;
    public decimal OldQuantity { get; } = section104History.OldQuantity;
    public decimal QuantityChange { get; } = section104History.QuantityChange;
    public decimal NewQuantity { get; } = section104History.OldQuantity + section104History.QuantityChange;
    public WrappedMoney OldValue { get; } = section104History.OldValue;
    public WrappedMoney ValueChange { get; } = section104History.ValueChange;
    public WrappedMoney NewValue { get; } = section104History.OldValue + section104History.ValueChange;
    public WrappedMoney OldContractValue { get; } = section104History.OldContractValue;
    public WrappedMoney ContractValueChange { get; } = section104History.ContractValueChange;
    public WrappedMoney NewContractValue { get; } = section104History.OldContractValue + section104History.ContractValueChange;
    public string Explaination { get; } = section104History.Explanation;

    public static IEnumerable<Section104HistoryViewModel?> GetSection104Data(UkSection104Pools section104Pools)
    {
        List<Section104HistoryViewModel?> displayList = [];
        List<UkSection104> section104List = section104Pools.GetSection104s();
        foreach (var section104 in section104List)
        {
            displayList.AddRange(section104.Section104HistoryList.Select(history => new Section104HistoryViewModel(history, section104.AssetName)));
        }
        return displayList.DefaultIfEmpty();
    }
}
