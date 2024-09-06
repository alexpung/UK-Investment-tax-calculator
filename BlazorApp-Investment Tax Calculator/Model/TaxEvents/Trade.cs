using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;

using System.Collections.Immutable;
using System.Text;
using System.Text.Json.Serialization;

namespace InvestmentTaxCalculator.Model.TaxEvents;
[JsonDerivedType(typeof(Trade), "trade")]
[JsonDerivedType(typeof(FxTrade), "fxTrade")]
[JsonDerivedType(typeof(FutureContractTrade), "futureContractTrade")]
public record Trade : TaxEvent, ITextFilePrintable
{
    public virtual AssetCatagoryType AssetType { get; set; } = AssetCatagoryType.STOCK;
    public virtual required TradeType AcquisitionDisposal { get; set; }
    public virtual required decimal Quantity { get; set; }
    public virtual required DescribedMoney GrossProceed { get; set; }
    public string Description { get; set; } = string.Empty;
    public ImmutableList<DescribedMoney> Expenses { get; init; } = [];
    public virtual WrappedMoney NetProceed
    {
        get
        {
            if (Expenses.IsEmpty) return GrossProceed.BaseCurrencyAmount;
            if (AcquisitionDisposal == TradeType.ACQUISITION) return GrossProceed.BaseCurrencyAmount + Expenses.Select(i => i.BaseCurrencyAmount).Sum();
            else return GrossProceed.BaseCurrencyAmount - Expenses.Select(i => i.BaseCurrencyAmount).Sum();
        }
    }

    public decimal RawQuantity => AcquisitionDisposal switch
    {
        TradeType.ACQUISITION => Quantity,
        TradeType.DISPOSAL => Quantity * -1,
        _ => throw new NotImplementedException($"Unknown trade type {AcquisitionDisposal}"),
    };

    protected string GetExpensesExplanation()
    {
        if (Expenses.IsEmpty) return string.Empty;
        StringBuilder stringBuilder = new();
        stringBuilder.Append("\n\tExpenses: ");
        foreach (var expense in Expenses)
        {
            stringBuilder.Append(expense.PrintToTextFile() + "\t");
        }
        return stringBuilder.ToString();
    }

    public virtual string PrintToTextFile()
    {
        string action = AcquisitionDisposal switch
        {
            TradeType.ACQUISITION => "Bought",
            TradeType.DISPOSAL => "Sold",
            _ => throw new NotImplementedException()
        };
        string netExplanation = AcquisitionDisposal switch
        {
            TradeType.ACQUISITION => $"Total cost: {NetProceed}",
            TradeType.DISPOSAL => $"Net proceed: {NetProceed}",
            _ => throw new NotImplementedException()
        };
        return $"{action} {Quantity} unit(s) of {AssetName} on {Date:dd-MMM-yyyy HH:mm} for {GrossProceed.PrintToTextFile()} " +
            $"with total expense {Expenses.Sum(expenses => expenses.BaseCurrencyAmount)}, {netExplanation}"
            + GetExpensesExplanation();
    }
}

