using InvestmentTaxCalculator.Model;

using Shouldly;

namespace UnitTest.Test.Model;

public class CountryCodeTest
{
    [Fact]
    public void GetAllRegions_ListsUnknownThenCommonRegionsThenRestAlphabetically()
    {
        IReadOnlyList<CountryCode> regions = CountryCode.GetAllRegions();
        int commonCount = CountryCode.CommonRegionCodes.Count;

        regions[0].ShouldBe(CountryCode.UnknownRegion);
        regions.Skip(1).Take(commonCount).Select(r => r.ThreeDigitCode).ShouldBe(CountryCode.CommonRegionCodes);

        List<string> remainingNames = regions.Skip(1 + commonCount).Select(r => r.CountryName).ToList();
        remainingNames.ShouldBe(remainingNames.OrderBy(n => n).ToList());
    }

    [Fact]
    public void GetAllRegions_ContainsEachRegionOnce()
    {
        IReadOnlyList<CountryCode> regions = CountryCode.GetAllRegions();

        regions.Select(r => r.ThreeDigitCode).Distinct().Count().ShouldBe(regions.Count);
        regions.ShouldContain(r => r.ThreeDigitCode == "TWN");
    }
}
