using InvestmentTaxCalculator.Model.Interfaces;

using System.Diagnostics.CodeAnalysis;

namespace InvestmentTaxCalculator.Model;

public record DescribedMoney : ITextFilePrintable
{
    public string Description { get; init; } = "";
    public required WrappedMoney Amount { get; init; }
    public decimal FxRate { get; init; } = 1;
    public WrappedMoney BaseCurrencyAmount => new(Amount.Amount * FxRate);

    public DescribedMoney() { }

    [SetsRequiredMembers]
    public DescribedMoney(decimal amount, string currency, decimal fxRate, string description = "")
    {
        Amount = new(amount, currency);
        FxRate = fxRate;
        Description = description;
    }

    public string PrintToTextFile()
    {
        string outputString;
        if (Description == string.Empty) outputString = $"{Amount}";
        else outputString = $"{Description}: {Amount}";
        if (FxRate == 1)
        {
            return outputString;
        }
        else return $"{outputString} = {BaseCurrencyAmount} Fx rate = {FxRate}";
    }

    public string Display()
    {
        if (FxRate == 1)
        {
            return BaseCurrencyAmount.ToString();
        }
        else return $"{BaseCurrencyAmount} ({Amount})";
    }
}
