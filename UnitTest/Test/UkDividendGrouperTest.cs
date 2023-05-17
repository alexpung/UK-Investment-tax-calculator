using CapitalGainCalculator.Model;
using CapitalGainCalculator.Model.Interfaces;
using CapitalGainCalculator.Model.UkTaxModel;
using Moq;
using NMoneys;
using Shouldly;
using System.Globalization;

namespace CapitalGainCalculator.Test;

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
        List<Dividend> data = new() {
            new Dividend()
            {
                AssetName = "MTR Corporation",
                CompanyLocation = new RegionInfo("HK"),
                DividendType = Enum.DividendType.DIVIDEND,
                Date = new DateTime(2022, 4, 5),
                Proceed = new DescribedMoney { Amount = new Money(1000, "HKD"), Description = "MTR Corporation dividend", FxRate = 0.11m }
            },
            new Dividend()
            {
                AssetName = "HSBC Bank",
                CompanyLocation = new RegionInfo("HK"),
                DividendType = Enum.DividendType.DIVIDEND_IN_LIEU,
                Date = new DateTime(2022, 4, 5),
                Proceed = new DescribedMoney { Amount = new Money(500, "HKD"), Description = "HSBC Bank dividend", FxRate = 0.11m }
            },
            new Dividend()
            {
                AssetName = "Shell",
                CompanyLocation = new RegionInfo("GB"),
                DividendType = Enum.DividendType.DIVIDEND,
                Date = new DateTime(2022, 4, 4),
                Proceed = new DescribedMoney { Amount = new Money(2000, "GBP"), Description = "Shell dividend", FxRate = 1m }
            },
            new Dividend()
            {
                AssetName = "Shell",
                CompanyLocation = new RegionInfo("GB"),
                DividendType = Enum.DividendType.WITHHOLDING,
                Date = new DateTime(2022, 4, 4),
                Proceed = new DescribedMoney { Amount = new Money(100, "GBP"), Description = "Shell withholding tax", FxRate = 1m }
            },
            new Dividend()
            {
                AssetName = "Sony Corporation",
                CompanyLocation = new RegionInfo("JP"),
                DividendType = Enum.DividendType.DIVIDEND,
                Date = new DateTime(2022, 4, 6),
                Proceed = new DescribedMoney { Amount = new Money(20000, "JPY"), Description = "Sony Corporation dividend", FxRate = 0.0063m }
            },
            new Dividend()
            {
                AssetName = "Sony Corporation",
                CompanyLocation = new RegionInfo("JP"),
                DividendType = Enum.DividendType.WITHHOLDING,
                Date = new DateTime(2022, 4, 6),
                Proceed = new DescribedMoney { Amount = new Money(3000, "JPY"), Description = "Sony Corporation withholding tax", FxRate = 0.0063m }
            },
            new Dividend()
            {
                AssetName = "Sony Corporation",
                CompanyLocation = new RegionInfo("JP"),
                DividendType = Enum.DividendType.DIVIDEND,
                Date = new DateTime(2022, 8, 6),
                Proceed = new DescribedMoney { Amount = new Money(10000, "JPY"), Description = "Sony Corporation dividend", FxRate = 0.007m }
            },
            new Dividend()
            {
                AssetName = "Sony Corporation",
                CompanyLocation = new RegionInfo("JP"),
                DividendType = Enum.DividendType.WITHHOLDING,
                Date = new DateTime(2022, 8, 6),
                Proceed = new DescribedMoney { Amount = new Money(1500, "JPY"), Description = "Sony Corporation withholding tax", FxRate = 0.007m }
            }
        };
        UkDividendCalculator calculator = SetUpCalculator(data);
        List<DividendSummary> result = calculator.CalculateTax();
        result.Count.ShouldBe(3);
        var hkResult = result.Single(i => i.CountryOfOrigin.Name == "HK");
        hkResult.TaxYear.ShouldBe(2021);
        hkResult.TotalTaxableDividend.ShouldBe(165);
        hkResult.TotalForeignTaxPaid.ShouldBe(0);
        var gbResult = result.Single(i => i.CountryOfOrigin.Name == "GB");
        gbResult.TaxYear.ShouldBe(2021);
        gbResult.TotalTaxableDividend.ShouldBe(2000);
        gbResult.TotalForeignTaxPaid.ShouldBe(100);
        var jpResult = result.Single(i => i.CountryOfOrigin.Name == "JP");
        jpResult.TaxYear.ShouldBe(2022);
        jpResult.TotalTaxableDividend.ShouldBe(196);
        jpResult.TotalForeignTaxPaid.ShouldBe(29.4m);
    }
}
