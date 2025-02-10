using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;

using System.Text.Json.Serialization;

namespace InvestmentTaxCalculator.Model.TaxEvents;

public record Dividend : TaxEvent, ITextFilePrintable
{
    public required DividendType DividendType { get; set; }
    [JsonIgnore]
    public CountryCode CompanyLocation { get; set; } = CountryCode.UnknownRegion;

    public string CompanyLocationName
    {
        get => CompanyLocation.CountryName;
        set => CompanyLocation = CountryCode.GetRegionByTwoDigitCode(value);
    }
    public required DescribedMoney Proceed { get; set; }

    public string PrintToTextFile()
    {
        return $"Asset Name: {AssetName}, " +
                $"Date: {Date.ToShortDateString()}, " +
                $"Type: {DividendType.GetDescription()}, " +
                $"Amount: {Proceed.Amount}, " +
                $"FxRate: {Proceed.FxRate}, " +
                $"Sterling Amount: {Proceed.BaseCurrencyAmount}, " +
                $"Description: {Proceed.Description}";
    }
}
