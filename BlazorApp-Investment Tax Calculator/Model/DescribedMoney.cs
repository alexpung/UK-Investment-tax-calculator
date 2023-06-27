using NMoneys;

namespace Model;

public record DescribedMoney
{
    public string Description { get; set; } = "";
    public required Money Amount { get; set; }
    public decimal FxRate { get; set; } = 1;

    public decimal BaseCurrencyAmount => Amount.Amount * FxRate;
}
