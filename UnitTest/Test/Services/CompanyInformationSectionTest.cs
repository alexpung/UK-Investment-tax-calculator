using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;
using InvestmentTaxCalculator.Services;
using InvestmentTaxCalculator.Services.PdfExport.Sections;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using System.Globalization;

namespace UnitTest.Test.Services;

public class CompanyInformationSectionTest
{
    [Fact]
    public void TestOnlyCompaniesWithEventsInTheTaxYearAreListed()
    {
        TaxEventLists taxEventLists = new();
        taxEventLists.Trades.Add(CreateTrade("ABC", "GB00AAAAAAAA", "01-May-21 10:00:00"));
        taxEventLists.Trades.Add(CreateTrade("XYZ", "US00BBBBBBBB", "01-May-19 10:00:00"));
        (CompanyInformationSection companyInformationSection, _, _, _, _) = CreateSection(taxEventLists);

        List<string> listedTickers = GetListedTickers(companyInformationSection, 2021);

        listedTickers.ShouldBe(["ABC"]);
    }

    [Fact]
    public void TestDividendInTaxYearMakesCompanyListed()
    {
        TaxEventLists taxEventLists = new();
        taxEventLists.Trades.Add(CreateTrade("ABC", "GB00AAAAAAAA", "01-May-19 10:00:00"));
        taxEventLists.Dividends.Add(CreateDividend("ABC", "GB00AAAAAAAA", "01-Jun-21 10:00:00"));
        (CompanyInformationSection companyInformationSection, _, _, _, _) = CreateSection(taxEventLists);

        GetListedTickers(companyInformationSection, 2021).ShouldBe(["ABC"]);
        GetListedTickers(companyInformationSection, 2019).ShouldBe(["ABC"]);
        GetListedTickers(companyInformationSection, 2020).ShouldBeEmpty();
    }

    [Fact]
    public void TestCompanyStillHeldAtEndOfTaxYearIsListedWithoutEventsInYear()
    {
        TaxEventLists taxEventLists = new();
        taxEventLists.Trades.Add(CreateTrade("HLD", "GB00CCCCCCCC", "01-May-19 10:00:00"));
        (CompanyInformationSection companyInformationSection, _, UkSection104Pools section104Pools, _, _) = CreateSection(taxEventLists);
        section104Pools.GetExistingOrInitialise("HLD")
            .AddAssets(DateTime.Parse("01-May-19 10:00:00", CultureInfo.InvariantCulture), 10, new WrappedMoney(100, "GBP"));

        GetListedTickers(companyInformationSection, 2021).ShouldBe(["HLD"]);
    }

    [Fact]
    public void TestCompanyDisposedInEarlierTaxYearIsNotListed()
    {
        TaxEventLists taxEventLists = new();
        taxEventLists.Trades.Add(CreateTrade("SLD", "GB00DDDDDDDD", "01-May-19 10:00:00"));
        (CompanyInformationSection companyInformationSection, _, UkSection104Pools section104Pools, _, _) = CreateSection(taxEventLists);
        UkSection104 section104 = section104Pools.GetExistingOrInitialise("SLD");
        section104.AddAssets(DateTime.Parse("01-May-19 10:00:00", CultureInfo.InvariantCulture), 10, new WrappedMoney(100, "GBP"));
        section104.ClearSection104(DateTime.Parse("01-Jun-19 10:00:00", CultureInfo.InvariantCulture), "Disposed");

        GetListedTickers(companyInformationSection, 2021).ShouldBeEmpty();
        GetListedTickers(companyInformationSection, 2019).ShouldBe(["SLD"]);
    }

    [Fact]
    public void TestTemporaryNonResidentDisposalListsCompanyInTheReportedTaxYear()
    {
        // A disposal made while temporarily non-resident is taxed (and reported) in the year residency resumes,
        // so the company belongs in that report year's company information even though the trade date is earlier.
        TaxEventLists taxEventLists = new();
        Trade disposalTrade = CreateTrade("TNR", "GB00EEEEEEEE", "01-May-20 10:00:00", TradeType.DISPOSAL);
        taxEventLists.Trades.Add(disposalTrade);
        (CompanyInformationSection companyInformationSection, _, _, TradeCalculationResult tradeCalculationResult,
            ResidencyStatusRecord residencyStatusRecord) = CreateSection(taxEventLists);
        residencyStatusRecord.SetResidencyStatus(new DateOnly(2019, 4, 6), new DateOnly(2023, 4, 5), ResidencyStatus.TemporaryNonResident);
        TradeTaxCalculation calculation = new([disposalTrade]) { ResidencyStatusAtTrade = ResidencyStatus.TemporaryNonResident };
        tradeCalculationResult.SetResult([calculation]);

        GetListedTickers(companyInformationSection, 2023).ShouldBe(["TNR"]);
        GetListedTickers(companyInformationSection, 2020).ShouldBe(["TNR"]);
        GetListedTickers(companyInformationSection, 2021).ShouldBeEmpty();
    }

    [Fact]
    public void TestTemporaryNonResidentAcquisitionListsCompanyInTheReportedTaxYear()
    {
        // An acquisition made while temporarily non-resident is grouped into the residency-resumption year and
        // appears in that year's "List of all trades" section, so its company belongs in that year's company
        // information too. Relevance therefore deliberately covers all calculated trades, not only disposals.
        TaxEventLists taxEventLists = new();
        Trade acquisitionTrade = CreateTrade("TNA", "GB00HHHHHHHH", "01-May-20 10:00:00");
        taxEventLists.Trades.Add(acquisitionTrade);
        (CompanyInformationSection companyInformationSection, _, _, TradeCalculationResult tradeCalculationResult,
            ResidencyStatusRecord residencyStatusRecord) = CreateSection(taxEventLists);
        residencyStatusRecord.SetResidencyStatus(new DateOnly(2019, 4, 6), new DateOnly(2023, 4, 5), ResidencyStatus.TemporaryNonResident);
        TradeTaxCalculation calculation = new([acquisitionTrade]) { ResidencyStatusAtTrade = ResidencyStatus.TemporaryNonResident };
        tradeCalculationResult.SetResult([calculation]);

        GetListedTickers(companyInformationSection, 2023).ShouldBe(["TNA"]);
        GetListedTickers(companyInformationSection, 2021).ShouldBeEmpty();
    }

    [Fact]
    public void TestTakeoverListsAcquiringCompanyInTheTaxYear()
    {
        // A takeover affects the acquiring company's Section 104 pool in the year of the action, so both companies
        // belong in that year's company information even when the acquiring company has no tax event of its own.
        TaxEventLists taxEventLists = new();
        taxEventLists.Trades.Add(CreateTrade("OLD", "GB00FFFFFFFF", "01-May-19 10:00:00"));
        taxEventLists.Trades.Add(CreateTrade("ACQ", "GB00GGGGGGGG", "01-May-19 10:00:00"));
        taxEventLists.CorporateActions.Add(new TakeoverCorporateAction
        {
            AssetName = "OLD",
            Date = DateTime.Parse("01-May-21 10:00:00", CultureInfo.InvariantCulture),
            AcquiringCompanyTicker = "ACQ",
            OldToNewRatio = 1m
        });
        (CompanyInformationSection companyInformationSection, _, _, _, _) = CreateSection(taxEventLists);

        GetListedTickers(companyInformationSection, 2021).ShouldBe(["ACQ", "OLD"]);
        GetListedTickers(companyInformationSection, 2020).ShouldBeEmpty();
    }

    private static (CompanyInformationSection Section, ShareIdentityRegistry Registry, UkSection104Pools Pools,
        TradeCalculationResult TradeCalculationResult, ResidencyStatusRecord ResidencyStatusRecord) CreateSection(TaxEventLists taxEventLists)
    {
        ShareIdentityRegistry shareIdentityRegistry = new();
        shareIdentityRegistry.RegisterEvents(taxEventLists.AllEvents);
        UKTaxYear ukTaxYear = new();
        ResidencyStatusRecord residencyStatusRecord = new();
        UkSection104Pools section104Pools = new(ukTaxYear, residencyStatusRecord, shareIdentityRegistry);
        TradeCalculationResult tradeCalculationResult = new(ukTaxYear, residencyStatusRecord);
        CompanyInformationSection companyInformationSection = new(shareIdentityRegistry, taxEventLists, ukTaxYear,
            section104Pools, tradeCalculationResult);
        return (companyInformationSection, shareIdentityRegistry, section104Pools, tradeCalculationResult, residencyStatusRecord);
    }

    /// <summary>
    /// Write the section for the given tax year and return the "Name in report" column of the company table,
    /// or an empty list when the section shows no table.
    /// </summary>
    private static List<string> GetListedTickers(CompanyInformationSection companyInformationSection, int taxYear)
    {
        Document document = new();
        Section section = document.AddSection();
        companyInformationSection.WriteSection(section, taxYear);
        Table? table = section.Elements.OfType<Table>().FirstOrDefault();
        if (table is null) return [];
        return [.. table.Rows.Cast<Row>()
            .Skip(1) // header row
            .Select(row => string.Concat(row.Cells[0].Elements.OfType<Paragraph>()
                .SelectMany(paragraph => paragraph.Elements.OfType<Text>())
                .Select(text => text.Content)))
            .Where(ticker => !string.IsNullOrEmpty(ticker))];
    }

    private static Trade CreateTrade(string assetName, string isin, string date, TradeType tradeType = TradeType.ACQUISITION)
    {
        return new Trade
        {
            AssetName = assetName,
            Isin = isin,
            Date = DateTime.Parse(date, CultureInfo.InvariantCulture),
            Quantity = 10,
            GrossProceed = new DescribedMoney(100, "GBP", 1),
            AcquisitionDisposal = tradeType
        };
    }

    private static Dividend CreateDividend(string assetName, string isin, string date)
    {
        return new Dividend
        {
            AssetName = assetName,
            Isin = isin,
            Date = DateTime.Parse(date, CultureInfo.InvariantCulture),
            DividendType = DividendType.DIVIDEND,
            Proceed = new DescribedMoney(100, "GBP", 1)
        };
    }
}
