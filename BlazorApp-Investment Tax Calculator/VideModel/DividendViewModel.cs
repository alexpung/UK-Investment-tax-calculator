using Enumerations;

using Model.TaxEvents;

namespace VideModel;

public record DividendViewModel(Dividend Dividend)
{
    public DateTime Date { get; } = Dividend.Date;
    public string DividendType { get; } = Dividend.DividendType.GetDescription();
    public string AssetName { get; } = Dividend.AssetName;
    public string CompanyLocaton { get; } = Dividend.CompanyLocation.EnglishName;
    public decimal SterlingAmount { get; } = Dividend.Proceed.BaseCurrencyAmount.Amount;
    public string Currency { get; set; } = Dividend.Proceed.Amount.Currency;
    public decimal LocalCurrencyAmount { get; set; } = Dividend.Proceed.Amount.Amount;
    public decimal ExchangeRate { get; } = Dividend.Proceed.FxRate;
    public string Description { get; } = Dividend.Proceed.Description;
}
