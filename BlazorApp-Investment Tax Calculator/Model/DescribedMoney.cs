using Model.Interfaces;
using NMoneys;

namespace Model;

public record DescribedMoney : ITextFilePrintable
{
    public string Description { get; set; } = "";
    public required Money Amount { get; set; }
    public decimal FxRate { get; set; } = 1;

    public Money BaseCurrencyAmount => BaseCurrencyMoney.BaseCurrencyAmount(Amount.Amount * FxRate);

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
}
