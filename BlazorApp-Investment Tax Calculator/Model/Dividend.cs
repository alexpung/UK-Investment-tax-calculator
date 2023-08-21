using Enum;
using Model.Interfaces;
using System.Globalization;

namespace Model;

public record Dividend : TaxEvent, ITextFilePrintable
{
    public required DividendType DividendType { get; set; }
    public RegionInfo CompanyLocation { get; set; } = RegionInfo.CurrentRegion;
    public required DescribedMoney Proceed { get; set; }

    public string PrintToTextFile()
    {
        return $"Asset Name: {AssetName}, " +
                $"Date: {Date.ToShortDateString()}, " +
                $"Type: {ToPrintedString(DividendType)}, " +
                $"Amount: {Proceed.Amount}, " +
                $"FxRate: {Proceed.FxRate}, " +
                $"Sterling Amount: {Proceed.BaseCurrencyAmount}, " +
                $"Description: {Proceed.Description}";
    }

    private static string ToPrintedString(DividendType dividendType) => dividendType switch
    {
        DividendType.WITHHOLDING => "Withholding Tax",
        DividendType.DIVIDEND_IN_LIEU => "Payment In Lieu of a Dividend",
        DividendType.DIVIDEND => "Dividend",
        _ => throw new NotImplementedException() //SHould not get a dividend object with any other type.
    };
}
