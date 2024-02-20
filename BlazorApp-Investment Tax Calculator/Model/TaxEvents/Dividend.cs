using Enumerations;

using Model.Interfaces;

using System.Globalization;
using System.Text.Json.Serialization;

namespace Model.TaxEvents;

public record Dividend : TaxEvent, ITextFilePrintable
{
    public required DividendType DividendType { get; set; }
    [JsonIgnore]
    public RegionInfo CompanyLocation { get; set; } = RegionInfo.CurrentRegion;

    public string CompanyLocationName
    {
        get => CompanyLocation.Name;
        set => CompanyLocation = new RegionInfo(value);
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
