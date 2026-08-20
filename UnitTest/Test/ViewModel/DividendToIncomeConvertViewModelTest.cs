using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Services;
using InvestmentTaxCalculator.ViewModel;

using System.Globalization;

namespace UnitTest.Test.ViewModel;

public class DividendToIncomeConvertViewModelTest
{
    [Fact]
    public void TestConvertedIncomeKeepsTheShareIdentityOfTheDividend()
    {
        // The dividend was paid under the former ticker, so its canonical name is the renamed one. The income
        // event replacing it must group under the same name, otherwise it stops matching the share it belongs to.
        Trade trade = CreateTrade("META", "US30303M1027", "01-Jun-24 10:00:00");
        Dividend dividend = CreateDividend("FB", "US30303M1027", "01-May-21 10:00:00");
        TaxEventLists taxEventLists = new();
        taxEventLists.AddData([trade, dividend]);
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents(taxEventLists.AllEvents);
        dividend.CanonicalAssetName.ShouldBe("META");

        DividendToIncomeConvertViewModel viewModel = new(taxEventLists);
        viewModel.ConvertDividendsToIncome(["META"]);

        taxEventLists.Dividends.ShouldBeEmpty();
        InterestIncome converted = taxEventLists.InterestIncomes.ShouldHaveSingleItem();
        converted.AssetName.ShouldBe("FB");
        converted.Isin.ShouldBe("US30303M1027");
        converted.ShareIdentity.ShouldBeSameAs(dividend.ShareIdentity);
        converted.CanonicalAssetName.ShouldBe("META");
        converted.IsSameAsset("META").ShouldBeTrue();
    }

    private static Trade CreateTrade(string assetName, string isin, string date) => new()
    {
        AssetName = assetName,
        Isin = isin,
        AssetType = AssetCategoryType.STOCK,
        AcquisitionDisposal = TradeType.ACQUISITION,
        Date = DateTime.Parse(date, CultureInfo.InvariantCulture),
        Quantity = 100,
        GrossProceed = new DescribedMoney(1000, "GBP", 1),
    };

    private static Dividend CreateDividend(string assetName, string isin, string date) => new()
    {
        AssetName = assetName,
        Isin = isin,
        Date = DateTime.Parse(date, CultureInfo.InvariantCulture),
        DividendType = DividendType.DIVIDEND,
        CompanyLocation = CountryCode.GetRegionByTwoDigitCode("US"),
        Proceed = new DescribedMoney(100, "GBP", 1),
    };
}
