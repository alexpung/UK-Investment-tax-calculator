using CapitalGainCalculator.Model;
using CapitalGainCalculator.Model.Interfaces;
using CapitalGainCalculator.Model.UkTaxModel;
using Moq;
using NMoneys;
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
    public void TestDividendGrouping()
    {
        Dividend dividend1 = new()
        {
            AssetName = "abc",
            CompanyLocation = new RegionInfo("HK"),
            DividendType = Enum.DividendType.DIVIDEND,
            Date = new DateTime(2022, 4, 5),
            Proceed = new DescribedMoney { Amount = new Money(1000, "HKD"), Description = "abc dividend", FxRate = 0.11m }
        };
        Dividend dividend2 = new()
        {
            AssetName = "HSBC bank",
            CompanyLocation = new RegionInfo("HK"),
            DividendType = Enum.DividendType.DIVIDEND_IN_LIEU,
            Date = new DateTime(2022, 4, 5),
            Proceed = new DescribedMoney { Amount = new Money(500, "HKD"), Description = "HSBC dividend", FxRate = 0.11m }
        };
        Dividend dividend3 = new()
        {
            AssetName = "def",
            CompanyLocation = new RegionInfo("GB"),
            DividendType = Enum.DividendType.DIVIDEND,
            Date = new DateTime(2022, 4, 4),
            Proceed = new DescribedMoney { Amount = new Money(2000, "GBP"), Description = "def dividend", FxRate = 1m }
        };
        Dividend dividend4 = new()
        {
            AssetName = "def",
            CompanyLocation = new RegionInfo("GB"),
            DividendType = Enum.DividendType.WITHHOLDING,
            Date = new DateTime(2022, 4, 4),
            Proceed = new DescribedMoney { Amount = new Money(100, "GBP"), Description = "def withholding tax", FxRate = 1m }
        };
        Dividend dividend5 = new()
        {
            AssetName = "def",
            CompanyLocation = new RegionInfo("JP"),
            DividendType = Enum.DividendType.DIVIDEND,
            Date = new DateTime(2022, 4, 6),
            Proceed = new DescribedMoney { Amount = new Money(20000, "JPY"), Description = "def dividend", FxRate = 0.0063m }
        };
        Dividend dividend6 = new()
        {
            AssetName = "def",
            CompanyLocation = new RegionInfo("JP"),
            DividendType = Enum.DividendType.WITHHOLDING,
            Date = new DateTime(2022, 4, 6),
            Proceed = new DescribedMoney { Amount = new Money(3000, "JPY"), Description = "def withholding tax", FxRate = 0.0063m }
        };
        List<Dividend> data = new() { dividend1, dividend2, dividend3, dividend4, dividend5, dividend6 };
        UkDividendCalculator calculator = SetUpCalculator(data);
        List<DividendSummary> result = calculator.CalculateTax();
    }
}
