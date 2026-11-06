using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Services;

using Shouldly;

using Xunit;

namespace UnitTest.Test.Model;

public class ReportingFundIncomeResolverTest
{
    private static readonly DateTime PeriodEnd = new(2023, 12, 31);
    private static readonly DateTime DistributionDate = new(2024, 6, 30);

    private static ExcessReportableIncome Eri(ExcessReportableIncomeType type = ExcessReportableIncomeType.DIVIDEND, decimal amount = 50m) => new()
    {
        AssetName = "VUSA",
        Date = DistributionDate,
        ReportingPeriodEndDate = PeriodEnd,
        IncomeType = type,
        Amount = new DescribedMoney(amount, "USD", 0.8m, "ERI")
    };

    private static FundEqualisation Equalisation(EqualisationIncomeSource? source, decimal amount, DateTime date, string currency = "USD", decimal fxRate = 0.8m) => new()
    {
        AssetName = "VUSA",
        Date = date,
        Amount = new DescribedMoney(amount, currency, fxRate, "Equalisation"),
        IncomeSource = source
    };

    private static Dividend Distribution(decimal amount, DateTime date) => new()
    {
        AssetName = "VUSA",
        Date = date,
        DividendType = DividendType.DIVIDEND,
        Proceed = new DescribedMoney(amount, "USD", 0.8m, "Dividend VUSA")
    };

    [Fact]
    public void Resolve_EriEntry_AddsIncomeWithoutChangingStoredLists()
    {
        TaxEventLists lists = new();
        lists.CorporateActions.Add(Eri(ExcessReportableIncomeType.DIVIDEND));
        lists.CorporateActions.Add(Eri(ExcessReportableIncomeType.INTEREST, 20m));

        ResolvedIncome resolved = ReportingFundIncomeResolver.Resolve(lists, lists.CorporateActions);

        Dividend dividend = resolved.Dividends.ShouldHaveSingleItem();
        dividend.DividendType.ShouldBe(DividendType.EXCESS_REPORTABLE_INCOME);
        dividend.Proceed.BaseCurrencyAmount.Amount.ShouldBe(40m);
        resolved.InterestIncomes.ShouldHaveSingleItem().InterestType.ShouldBe(InterestType.EXCESSREPORTABLEINCOME);
        lists.Dividends.ShouldBeEmpty();
        lists.InterestIncomes.ShouldBeEmpty();
    }

    [Fact]
    public void Resolve_EriEqualisation_ReducesDerivedEriIncome()
    {
        TaxEventLists lists = new();
        ExcessReportableIncome eri = Eri();
        FundEqualisation equalisation = Equalisation(EqualisationIncomeSource.ExcessReportableIncome, 10m, DistributionDate);
        lists.CorporateActions.AddRange([eri, equalisation]);

        ResolvedIncome resolved = ReportingFundIncomeResolver.Resolve(lists, lists.CorporateActions);

        resolved.Dividends.ShouldHaveSingleItem().Proceed.Amount.Amount.ShouldBe(40m);
        resolved.UnmatchedEqualisations.ShouldBeEmpty();
        equalisation.LinkedExcessReportableIncome.ShouldBeSameAs(eri);
        eri.Amount.Amount.Amount.ShouldBe(50m);
    }

    [Fact]
    public void Resolve_DistributionEqualisation_ReducesCopyNotStoredDividend()
    {
        TaxEventLists lists = new();
        Dividend stored = Distribution(100m, new DateTime(2024, 3, 1));
        lists.Dividends.Add(stored);
        lists.CorporateActions.Add(Equalisation(EqualisationIncomeSource.Distribution, 12.5m, stored.Date));
        lists.CorporateActions.Add(Equalisation(EqualisationIncomeSource.Distribution, 7.5m, stored.Date));

        ResolvedIncome resolved = ReportingFundIncomeResolver.Resolve(lists, lists.CorporateActions);

        Dividend reduced = resolved.Dividends.ShouldHaveSingleItem();
        reduced.Proceed.Amount.Amount.ShouldBe(80m);
        reduced.Id.ShouldBe(stored.Id);
        stored.Proceed.Amount.Amount.ShouldBe(100m);
        stored.Proceed.Description.ShouldBe("Dividend VUSA");
    }

    [Fact]
    public void Resolve_DistributionConvertedToInterest_StillReduced()
    {
        TaxEventLists lists = new();
        lists.InterestIncomes.Add(new InterestIncome
        {
            AssetName = "VUSA",
            Date = new DateTime(2024, 3, 1),
            InterestType = InterestType.ETFDIVIDEND,
            Amount = new DescribedMoney(100m, "USD", 0.8m, "ETF dividend")
        });
        lists.CorporateActions.Add(Equalisation(EqualisationIncomeSource.Distribution, 10m, new DateTime(2024, 3, 1)));

        ResolvedIncome resolved = ReportingFundIncomeResolver.Resolve(lists, lists.CorporateActions);

        resolved.InterestIncomes.ShouldHaveSingleItem().Amount.Amount.Amount.ShouldBe(90m);
        lists.InterestIncomes.Single().Amount.Amount.Amount.ShouldBe(100m);
    }

    [Fact]
    public void Resolve_EqualisationWithoutIncome_IsReportedUnmatched()
    {
        TaxEventLists lists = new();
        FundEqualisation orphan = Equalisation(EqualisationIncomeSource.ExcessReportableIncome, 10m, DistributionDate);
        lists.CorporateActions.Add(orphan);
        lists.Dividends.Add(Distribution(100m, DistributionDate));

        ResolvedIncome resolved = ReportingFundIncomeResolver.Resolve(lists, lists.CorporateActions);

        // An ERI equalisation never reduces an ordinary distribution on the same date.
        resolved.Dividends.ShouldHaveSingleItem().Proceed.Amount.Amount.ShouldBe(100m);
        resolved.UnmatchedEqualisations.ShouldHaveSingleItem().ShouldBe(orphan);
    }

    [Fact]
    public void EffectiveReportingPeriodEnd_FollowsEditedEri()
    {
        TaxEventLists lists = new();
        FundEqualisation equalisation = Equalisation(EqualisationIncomeSource.ExcessReportableIncome, 10m, DistributionDate);
        lists.CorporateActions.AddRange([Eri(), equalisation]);
        ReportingFundIncomeResolver.LinkEqualisations(lists.CorporateActions);
        equalisation.EffectiveReportingPeriodEndDate.ShouldBe(DateOnly.FromDateTime(PeriodEnd));

        // Editing the ERI replaces it; the next calculation links the equalisation to the new entry.
        lists.CorporateActions[0] = Eri() with { ReportingPeriodEndDate = new DateTime(2023, 12, 15) };
        ReportingFundIncomeResolver.LinkEqualisations(lists.CorporateActions);

        equalisation.EffectiveReportingPeriodEndDate.ShouldBe(new DateOnly(2023, 12, 15));
    }

    [Fact]
    public void Equality_IgnoresCalculationTimeLink()
    {
        FundEqualisation equalisation = Equalisation(EqualisationIncomeSource.ExcessReportableIncome, 10m, DistributionDate);
        List<CorporateAction> actions = [equalisation];
        int hashBefore = equalisation.GetHashCode();

        equalisation.LinkedExcessReportableIncome = Eri();

        equalisation.GetHashCode().ShouldBe(hashBefore);
        actions.Remove(equalisation).ShouldBeTrue();
    }

    [Fact]
    public void DividendCalculator_RunRepeatedly_DoesNotAccumulateDerivedIncome()
    {
        TaxEventLists lists = new();
        Dividend stored = Distribution(100m, new DateTime(2024, 3, 1));
        lists.Dividends.Add(stored);
        lists.CorporateActions.Add(Eri());
        lists.CorporateActions.Add(Equalisation(EqualisationIncomeSource.ExcessReportableIncome, 10m, DistributionDate));
        lists.CorporateActions.Add(Equalisation(EqualisationIncomeSource.Distribution, 10m, stored.Date));
        ToastService toastService = NSubstitute.Substitute.For<ToastService>(NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<ToastService>>());
        UkDividendCalculator calculator = new(lists, new UKTaxYear(), toastService, new ResidencyStatusRecord(), lists);

        List<List<DividendSummary>> runs = [calculator.CalculateTax(), calculator.CalculateTax(), calculator.CalculateTax()];

        // The stored data is exactly what was entered, however many times the calculation runs.
        lists.Dividends.ShouldHaveSingleItem().ShouldBeSameAs(stored);
        stored.Proceed.Amount.Amount.ShouldBe(100m);
        lists.InterestIncomes.ShouldBeEmpty();
        lists.CorporateActions.Count.ShouldBe(3);
        // Every run gives the same income: ERI 50 USD - 10 USD equalisation, distribution 100 USD - 10 USD, at 0.8.
        foreach (List<DividendSummary> summaries in runs)
        {
            summaries.SelectMany(s => s.RelatedDividendsAndTaxes).Count().ShouldBe(2);
            summaries.Sum(s => s.TotalTaxableDividend).Amount.ShouldBe(104m);
            summaries.Sum(s => s.TotalExcessReportableIncomeDividend).Amount.ShouldBe(32m);
        }
    }

    [Fact]
    public void LegacyMigration_RemovesGeneratedEriIncomeAndRestoresReducedDistribution()
    {
        // As saved by older versions: ERI income stored as a dividend and reduced by an ERI equalisation, and a
        // regular dividend already reduced by a distribution equalisation.
        ExcessReportableIncome eri = Eri();
        Dividend generatedEri = new()
        {
            AssetName = "VUSA",
            Date = DistributionDate,
            DividendType = DividendType.EXCESS_REPORTABLE_INCOME,
            Proceed = new DescribedMoney(40m, "USD", 0.8m, "Excess Reportable Income (Dividend) from VUSA (Reduced by equalisation of £8.00)")
        };
        FundEqualisation eriEqualisation = Equalisation(null, 10m, DistributionDate) with
        {
            RelatedEventDescription = "Excess Reportable Income (Dividend) on 30/06/2024",
            ReportingPeriodEndDate = PeriodEnd
        };
        FundEqualisation distributionEqualisation = Equalisation(null, 10m, new DateTime(2024, 3, 1)) with { RelatedEventDescription = "Dividend on 01/03/2024" };
        Dividend reduced = Distribution(90m, new DateTime(2024, 3, 1)) with
        {
            Proceed = new DescribedMoney(90m, "USD", 0.8m, "Dividend VUSA" + $" (Reduced by equalisation of {distributionEqualisation.Amount.BaseCurrencyAmount})")
        };
        TaxEventLists lists = new();
        lists.CorporateActions.AddRange([eri, eriEqualisation, distributionEqualisation]);
        lists.Dividends.AddRange([generatedEri, reduced]);

        ReportingFundLegacyMigration.Migrate(lists);

        Dividend restored = lists.Dividends.ShouldHaveSingleItem();
        restored.Proceed.Amount.Amount.ShouldBe(100m);
        restored.Proceed.Description.ShouldBe("Dividend VUSA");
        List<FundEqualisation> migrated = [.. lists.CorporateActions.OfType<FundEqualisation>()];
        migrated.Select(e => e.IncomeSource).ShouldBe([EqualisationIncomeSource.ExcessReportableIncome, EqualisationIncomeSource.Distribution]);

        // The calculation now derives the same taxable income as before migration.
        ResolvedIncome resolved = ReportingFundIncomeResolver.Resolve(lists, lists.CorporateActions);
        resolved.Dividends.Single(d => d.DividendType == DividendType.EXCESS_REPORTABLE_INCOME).Proceed.Amount.Amount.ShouldBe(40m);
        resolved.Dividends.Single(d => d.DividendType == DividendType.DIVIDEND).Proceed.Amount.Amount.ShouldBe(90m);

        // Migrating again changes nothing.
        ReportingFundLegacyMigration.Migrate(lists);
        lists.Dividends.ShouldHaveSingleItem().Proceed.Amount.Amount.ShouldBe(100m);
    }
}
