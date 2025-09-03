using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.ViewModel;

public record DividendViewModel(Dividend Dividend)
{
    public DateTime Date { get; } = Dividend.Date;
    public string DividendType { get; } = Dividend.DividendType.GetDescription();
    public string AssetName { get; } = Dividend.AssetName;
    public string CompanyLocaton { get; } = Dividend.CompanyLocation.CountryName;
    public WrappedMoney SterlingAmount { get; } = Dividend.Proceed.BaseCurrencyAmount;
    public string Currency { get; set; } = Dividend.Proceed.Amount.Currency;
    public WrappedMoney LocalCurrencyAmount { get; set; } = Dividend.Proceed.Amount;
    public decimal ExchangeRate { get; } = Dividend.Proceed.FxRate;
    public string Description { get; } = Dividend.Proceed.Description;
}
