using Enumerations;

using Model;
using Model.Interfaces;
using Model.TaxEvents;
using Model.UkTaxModel;

using Moq;

using System.Globalization;

namespace UnitTest.Test.Model;

public class UkDividendCalculatorTest
{
    private static UkDividendCalculator SetUpCalculator(List<Dividend> Dividend)
    {
        Mock<IDividendLists> mockIDividendLists = new();
        mockIDividendLists.Setup(f => f.Dividends).Returns(Dividend);
        return new UkDividendCalculator(mockIDividendLists.Object, new UKTaxYear());
    }

    [Fact]
    public void TestDividendCalculation()
    {
        List<Dividend> data = [
            new Dividend()
            {
                AssetName = "MTR Corporation",
                CompanyLocation = new RegionInfo("HK"),
                DividendType = DividendType.DIVIDEND,
                Date = new DateTime(2022, 4, 5, 0, 0, 0, DateTimeKind.Local),
                Proceed = new DescribedMoney { Amount = new WrappedMoney(1000, "HKD"), Description = "MTR Corporation dividend", FxRate = 0.11m }
            },
            new Dividend()
            {
                AssetName = "HSBC Bank",
                CompanyLocation = new RegionInfo("HK"),
                DividendType = DividendType.DIVIDEND_IN_LIEU,
                Date = new DateTime(2022, 4, 5, 0, 0, 0, DateTimeKind.Local),
                Proceed = new DescribedMoney { Amount = new WrappedMoney(500, "HKD"), Description = "HSBC Bank dividend", FxRate = 0.11m }
            },
            new Dividend()
            {
                AssetName = "Shell",
                CompanyLocation = new RegionInfo("GB"),
                DividendType = DividendType.DIVIDEND,
                Date = new DateTime(2022, 4, 4, 0, 0, 0, DateTimeKind.Local),
                Proceed = new DescribedMoney { Amount = new WrappedMoney(2000, "GBP"), Description = "Shell dividend", FxRate = 1m }
            },
            new Dividend()
            {
                AssetName = "Shell",
                CompanyLocation = new RegionInfo("GB"),
                DividendType = DividendType.WITHHOLDING,
                Date = new DateTime(2022, 4, 4, 0, 0, 0, DateTimeKind.Local),
                Proceed = new DescribedMoney { Amount = new WrappedMoney(100, "GBP"), Description = "Shell withholding tax", FxRate = 1m }
            },
            new Dividend()
            {
                AssetName = "Sony Corporation",
                CompanyLocation = new RegionInfo("JP"),
                DividendType = DividendType.DIVIDEND,
                Date = new DateTime(2022, 4, 6, 0, 0, 0, DateTimeKind.Local),
                Proceed = new DescribedMoney { Amount = new WrappedMoney(20000, "JPY"), Description = "Sony Corporation dividend", FxRate = 0.0063m }
            },
            new Dividend()
            {
                AssetName = "Sony Corporation",
                CompanyLocation = new RegionInfo("JP"),
                DividendType = DividendType.WITHHOLDING,
                Date = new DateTime(2022, 4, 6, 0, 0, 0, DateTimeKind.Local),
                Proceed = new DescribedMoney { Amount = new WrappedMoney(3000, "JPY"), Description = "Sony Corporation withholding tax", FxRate = 0.0063m }
            },
            new Dividend()
            {
                AssetName = "Sony Corporation",
                CompanyLocation = new RegionInfo("JP"),
                DividendType = DividendType.DIVIDEND,
                Date = new DateTime(2022, 8, 6, 0, 0, 0, DateTimeKind.Local),
                Proceed = new DescribedMoney { Amount = new WrappedMoney(10000, "JPY"), Description = "Sony Corporation dividend", FxRate = 0.007m }
            },
            new Dividend()
            {
                AssetName = "Sony Corporation",
                CompanyLocation = new RegionInfo("JP"),
                DividendType = DividendType.WITHHOLDING,
                Date = new DateTime(2022, 8, 6, 0, 0, 0, DateTimeKind.Local),
                Proceed = new DescribedMoney { Amount = new WrappedMoney(1500, "JPY"), Description = "Sony Corporation withholding tax", FxRate = 0.007m }
            }
        ];
        UkDividendCalculator calculator = SetUpCalculator(data);
        List<DividendSummary> result = calculator.CalculateTax();
        result.Count.ShouldBe(3);
        var hkResult = result.Single(i => i.CountryOfOrigin.Name == "HK");
        hkResult.TaxYear.ShouldBe(2021);
        hkResult.TotalTaxableDividend.ShouldBe(new WrappedMoney(165m));
        hkResult.TotalForeignTaxPaid.ShouldBe(new WrappedMoney(0m));
        var gbResult = result.Single(i => i.CountryOfOrigin.Name == "GB");
        gbResult.TaxYear.ShouldBe(2021);
        gbResult.TotalTaxableDividend.ShouldBe(new WrappedMoney(2000m));
        gbResult.TotalForeignTaxPaid.ShouldBe(new WrappedMoney(100m));
        var jpResult = result.Single(i => i.CountryOfOrigin.Name == "JP");
        jpResult.TaxYear.ShouldBe(2022);
        jpResult.TotalTaxableDividend.ShouldBe(new WrappedMoney(196m));
        jpResult.TotalForeignTaxPaid.ShouldBe(new WrappedMoney(29.4m));
    }
}
