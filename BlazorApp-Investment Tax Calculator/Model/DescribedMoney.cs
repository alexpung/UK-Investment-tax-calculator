using NMoneys;

namespace Model;

public record DescribedMoney
{
    public string Description { get; set; } = "";
    public required Money Amount { get; set; }
    public decimal FxRate { get; set; } = 1;

    public Money BaseCurrencyAmount => BaseCurrencyMoney.BaseCurrencyAmount(Amount.Amount * FxRate);
}
