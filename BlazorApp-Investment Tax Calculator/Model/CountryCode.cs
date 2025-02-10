using System.Globalization;

namespace InvestmentTaxCalculator.Model;

public record CountryCode(string TwoDigitCode, string ThreeDigitCode, string CountryName)
{
    public static readonly CountryCode UnknownRegion = new("ZZ", "ZZZ", "Unknown Region");

    public static CountryCode GetRegionByTwoDigitCode(string twoDigitCode)
    {
        try
        {
            RegionInfo regionInfo = new(twoDigitCode);
            return new CountryCode(twoDigitCode, regionInfo.ThreeLetterISORegionName, regionInfo.EnglishName);
        }
        catch (ArgumentException)
        {
            return UnknownRegion;
        }
    }
}