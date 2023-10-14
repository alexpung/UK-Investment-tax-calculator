using Model.Interfaces;

namespace Model;

public record DescribedMoney : ITextFilePrintable
{
    public string Description { get; set; } = "";
    public required WrappedMoney Amount { get; set; }
    public decimal FxRate { get; set; } = 1;

    public WrappedMoney BaseCurrencyAmount => new(Amount.Amount * FxRate);

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
