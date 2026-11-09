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
    public void EffectiveReportingPeriodEnd_FollowsLinkedEri()
    {
        TaxEventLists lists = new();
        FundEqualisation equalisation = Equalisation(EqualisationIncomeSource.ExcessReportableIncome, 10m, DistributionDate);
        lists.CorporateActions.AddRange([Eri(), equalisation]);
        ReportingFundIncomeResolver.LinkEqualisations(lists.CorporateActions);
        equalisation.EffectiveReportingPeriodEndDate.ShouldBe(DateOnly.FromDateTime(PeriodEnd));

        // A replacement ERI entry (deleted and re-added) is picked up by the next calculation.
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
    public void DeletingEri_AlsoDeletesItsEqualisations()
    {
        TaxEventLists lists = new();
        ExcessReportableIncome eri = Eri();
        FundEqualisation eriEqualisation = Equalisation(EqualisationIncomeSource.ExcessReportableIncome, 10m, DistributionDate);
        Dividend distribution = Distribution(100m, DistributionDate);
        FundEqualisation distributionEqualisation = Equalisation(EqualisationIncomeSource.Distribution, 5m, DistributionDate);
        lists.Dividends.Add(distribution);
        lists.CorporateActions.AddRange([eri, eriEqualisation, distributionEqualisation]);

        IncomeChangeResult result = EqualisationIntegrity.ApplyIncomeChange(lists, () => lists.CorporateActions.Remove(eri));
        List<FundEqualisation> removed = result.RemovedEqualisations;

        removed.ShouldHaveSingleItem().ShouldBe(eriEqualisation);
        // The distribution equalisation on the same date belongs to the dividend, not the ERI, so it stays.
        lists.CorporateActions.ShouldHaveSingleItem().ShouldBe(distributionEqualisation);
        result.RemovedEqualisationsWarning.ShouldNotBeNull().ShouldContain("1 fund equalisation entry");
    }

    [Fact]
    public void DeletingDividend_AlsoDeletesItsEqualisations()
    {
        TaxEventLists lists = new();
        Dividend distribution = Distribution(100m, new DateTime(2024, 3, 1));
        FundEqualisation equalisation = Equalisation(EqualisationIncomeSource.Distribution, 5m, distribution.Date);
        lists.Dividends.Add(distribution);
        lists.CorporateActions.Add(equalisation);

        List<FundEqualisation> removed = EqualisationIntegrity.ApplyIncomeChange(lists, () => lists.Dividends.Remove(distribution)).RemovedEqualisations;

        removed.ShouldHaveSingleItem().ShouldBe(equalisation);
        lists.CorporateActions.ShouldBeEmpty();
    }

    [Fact]
    public void EditingDividend_KeepsEqualisationWhileStillMatched_DeletesItWhenMovedToAnotherDate()
    {
        TaxEventLists lists = new();
        Dividend original = Distribution(100m, new DateTime(2024, 3, 1));
        FundEqualisation equalisation = Equalisation(EqualisationIncomeSource.Distribution, 5m, original.Date);
        lists.Dividends.Add(original);
        lists.CorporateActions.Add(equalisation);

        // Changing only the amount (remove and re-add, as the manual entry form does) keeps the equalisation.
        Dividend newAmount = Distribution(120m, original.Date);
        EqualisationIntegrity.ApplyIncomeChange(lists, () => { lists.Dividends.Remove(original); lists.Dividends.Add(newAmount); }).RemovedEqualisations
            .ShouldBeEmpty();
        lists.CorporateActions.ShouldHaveSingleItem();

        Dividend newDate = Distribution(120m, new DateTime(2024, 4, 1));
        EqualisationIntegrity.ApplyIncomeChange(lists, () => { lists.Dividends.Remove(newAmount); lists.Dividends.Add(newDate); }).RemovedEqualisations
            .ShouldHaveSingleItem().ShouldBe(equalisation);
        lists.CorporateActions.ShouldBeEmpty();
    }

    [Fact]
    public void Deleting_LeavesEqualisationsThatAlreadyHadNoIncome()
    {
        // e.g. imported with "Load Dividends" unchecked: unrelated deletes must not remove it.
        TaxEventLists lists = new();
        FundEqualisation alreadyUnmatched = Equalisation(EqualisationIncomeSource.Distribution, 5m, new DateTime(2024, 3, 1));
        ExcessReportableIncome eri = Eri();
        lists.CorporateActions.AddRange([alreadyUnmatched, eri]);

        EqualisationIntegrity.ApplyIncomeChange(lists, () => lists.CorporateActions.Remove(eri)).RemovedEqualisations.ShouldBeEmpty();

        lists.CorporateActions.ShouldHaveSingleItem().ShouldBe(alreadyUnmatched);
    }

    [Fact]
    public void EffectiveReportingPeriodEnd_WithoutEri_UsesMonthEndFallback()
    {
        FundEqualisation equalisation = Equalisation(EqualisationIncomeSource.ExcessReportableIncome, 10m, new DateTime(2024, 6, 30));

        equalisation.EffectiveReportingPeriodEndDate.ShouldBe(new DateOnly(2023, 12, 31));
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

    [Fact]
    public void FindDistribution_PrefersRecordedIncomeType_OverLargerDistributionSameDay()
    {
        TaxEventLists lists = new();
        DateTime date = new(2024, 3, 1);
        Dividend dividend = Distribution(500m, date);
        InterestIncome interest = new()
        {
            AssetName = "VUSA",
            Date = date,
            InterestType = InterestType.ETFDIVIDEND,
            Amount = new DescribedMoney(100m, "USD", 0.8m, "Bond fund distribution")
        };
        lists.Dividends.Add(dividend);
        lists.InterestIncomes.Add(interest);
        FundEqualisation equalisation = Equalisation(EqualisationIncomeSource.Distribution, 10m, date) with { IncomeEventType = nameof(InterestType.ETFDIVIDEND) };
        lists.CorporateActions.Add(equalisation);

        ReportingFundIncomeResolver.FindIncome(equalisation, lists).ShouldBeSameAs(interest);
        ResolvedIncome resolved = ReportingFundIncomeResolver.Resolve(lists, lists.CorporateActions);
        resolved.InterestIncomes.ShouldHaveSingleItem().Amount.Amount.Amount.ShouldBe(90m);
        resolved.Dividends.ShouldHaveSingleItem().Proceed.Amount.Amount.ShouldBe(500m);
    }

    [Fact]
    public void FindExcessReportableIncome_PrefersRecordedIncomeType()
    {
        TaxEventLists lists = new();
        ExcessReportableIncome dividendEri = Eri(ExcessReportableIncomeType.DIVIDEND);
        ExcessReportableIncome interestEri = Eri(ExcessReportableIncomeType.INTEREST, 20m);
        lists.CorporateActions.AddRange([dividendEri, interestEri]);
        FundEqualisation equalisation = Equalisation(EqualisationIncomeSource.ExcessReportableIncome, 5m, DistributionDate) with { IncomeEventType = nameof(ExcessReportableIncomeType.INTEREST) };

        ReportingFundIncomeResolver.FindExcessReportableIncome(equalisation, lists.CorporateActions).ShouldBeSameAs(interestEri);
    }

    [Fact]
    public void AccruedInterest_IsNotAFundDistribution()
    {
        InterestIncome accrued = new()
        {
            AssetName = "VUSA",
            Date = new DateTime(2024, 3, 1),
            InterestType = InterestType.ACCURREDINCOMEPROFIT,
            Amount = new DescribedMoney(10m, "GBP", 1m)
        };

        ReportingFundIncomeResolver.IsDistribution(accrued).ShouldBeFalse();
    }

    [Fact]
    public void ApplyIncomeChange_EditBelowEqualisation_IsRejectedAndUndone()
    {
        TaxEventLists lists = new();
        Dividend original = Distribution(100m, new DateTime(2024, 3, 1));
        FundEqualisation equalisation = Equalisation(EqualisationIncomeSource.Distribution, 50m, original.Date);
        lists.Dividends.Add(original);
        lists.CorporateActions.Add(equalisation);
        Dividend smaller = Distribution(40m, original.Date);

        IncomeChangeResult result = EqualisationIntegrity.ApplyIncomeChange(lists,
            () => { lists.Dividends.Remove(original); lists.Dividends.Add(smaller); }, rejectOverEqualisation: true);

        result.Applied.ShouldBeFalse();
        result.Error.ShouldNotBeNull().ShouldContain("smaller than the fund equalisation");
        lists.Dividends.ShouldHaveSingleItem().ShouldBeSameAs(original);
        lists.CorporateActions.ShouldHaveSingleItem().ShouldBe(equalisation);
    }

    [Fact]
    public void ApplyIncomeChange_EditStillCoveringEqualisation_IsApplied()
    {
        TaxEventLists lists = new();
        Dividend original = Distribution(100m, new DateTime(2024, 3, 1));
        lists.Dividends.Add(original);
        lists.CorporateActions.Add(Equalisation(EqualisationIncomeSource.Distribution, 50m, original.Date));
        Dividend smaller = Distribution(60m, original.Date);

        IncomeChangeResult result = EqualisationIntegrity.ApplyIncomeChange(lists,
            () => { lists.Dividends.Remove(original); lists.Dividends.Add(smaller); }, rejectOverEqualisation: true);

        result.Applied.ShouldBeTrue();
        lists.Dividends.ShouldHaveSingleItem().ShouldBeSameAs(smaller);
        lists.CorporateActions.ShouldHaveSingleItem();
    }

    [Fact]
    public void ImportFilter_DividendsNotLoaded_KeepsEriForSection104ButExcludesItsIncome()
    {
        TaxEventLists imported = new();
        imported.CorporateActions.Add(Eri(ExcessReportableIncomeType.DIVIDEND));
        imported.CorporateActions.Add(Eri(ExcessReportableIncomeType.INTEREST, 20m));
        imported.CorporateActions.Add(Equalisation(EqualisationIncomeSource.ExcessReportableIncome, 10m, DistributionDate) with { IncomeEventType = "DIVIDEND" });
        AssetTypeToLoadSetting setting = new() { LoadDividends = false };

        TaxEventLists filtered = setting.FilterTaxEvent(imported);

        List<ExcessReportableIncome> eris = [.. filtered.CorporateActions.OfType<ExcessReportableIncome>()];
        eris.Single(e => e.IncomeType == ExcessReportableIncomeType.DIVIDEND).IsIncomeExcluded.ShouldBeTrue();
        eris.Single(e => e.IncomeType == ExcessReportableIncomeType.INTEREST).IsIncomeExcluded.ShouldBeFalse();
        ResolvedIncome resolved = ReportingFundIncomeResolver.Resolve(filtered, filtered.CorporateActions);
        resolved.Dividends.ShouldBeEmpty();
        resolved.InterestIncomes.ShouldHaveSingleItem().Amount.Amount.Amount.ShouldBe(20m);
        // The equalisation still has its ERI (its cost reduction applies) but has no income to reduce.
        resolved.UnmatchedEqualisations.ShouldBeEmpty();
    }

    [Fact]
    public void LegacyMigration_MatchesIncomeRecordedUnderAnotherTickerOfTheSameShare()
    {
        // The ERI and its equalisation use one ticker, the stored income another; the shared ISIN links them.
        const string isin = "IE00B3XXRP09";
        ExcessReportableIncome eri = Eri() with { Isin = isin };
        Dividend generatedEri = new()
        {
            AssetName = "VUSA.L",
            Isin = isin,
            Date = DistributionDate,
            DividendType = DividendType.EXCESS_REPORTABLE_INCOME,
            Proceed = new DescribedMoney(50m, "USD", 0.8m, "Excess Reportable Income (Dividend) from VUSA")
        };
        FundEqualisation distributionEqualisation = Equalisation(null, 10m, new DateTime(2024, 3, 1)) with { RelatedEventDescription = "Dividend on 01/03/2024", Isin = isin };
        Dividend reduced = new()
        {
            AssetName = "VUSA.L",
            Isin = isin,
            Date = new DateTime(2024, 3, 1),
            DividendType = DividendType.DIVIDEND,
            Proceed = new DescribedMoney(90m, "USD", 0.8m, "Dividend VUSA" + $" (Reduced by equalisation of {distributionEqualisation.Amount.BaseCurrencyAmount})")
        };
        TaxEventLists lists = new();
        lists.CorporateActions.AddRange([eri, distributionEqualisation]);
        lists.Dividends.AddRange([generatedEri, reduced]);

        ReportingFundLegacyMigration.Migrate(lists);

        Dividend restored = lists.Dividends.ShouldHaveSingleItem();
        restored.Proceed.Amount.Amount.ShouldBe(100m);
        lists.CorporateActions.OfType<FundEqualisation>().Single().IncomeEventType.ShouldBe(nameof(DividendType.DIVIDEND));
    }

    [Fact]
    public void SkipDuplicates_ReimportWithIncomeLoaded_ClearsExclusionWithoutAddingSecondEri()
    {
        TaxEventLists lists = new();
        lists.CorporateActions.Add(Eri() with { IsIncomeExcluded = true });
        TaxEventLists reimport = new();
        reimport.CorporateActions.Add(Eri());

        lists.AddData(reimport, skipDuplicates: true);

        lists.CorporateActions.ShouldHaveSingleItem().ShouldBeOfType<ExcessReportableIncome>().IsIncomeExcluded.ShouldBeFalse();
    }

    [Fact]
    public void ApplyIncomeChange_RejectedChange_RestoresInPlaceAmountEdit()
    {
        TaxEventLists lists = new();
        Dividend dividend = Distribution(100m, new DateTime(2024, 3, 1));
        lists.Dividends.Add(dividend);
        lists.CorporateActions.Add(Equalisation(EqualisationIncomeSource.Distribution, 50m, dividend.Date));

        IncomeChangeResult result = EqualisationIntegrity.ApplyIncomeChange(lists,
            () => dividend.Proceed = new DescribedMoney(40m, "USD", 0.8m, "Edited in place"), rejectOverEqualisation: true);

        result.Applied.ShouldBeFalse();
        dividend.Proceed.Amount.Amount.ShouldBe(100m);
    }

    [Fact]
    public void ApplyIncomeChange_RejectsEditWorseningExistingShortfall_AllowsOneThatDoesNot()
    {
        // £150 of equalisation (USD 150 at 0.8 = £120) already exceeds the £80 income (USD 100).
        TaxEventLists lists = new();
        Dividend dividend = Distribution(100m, new DateTime(2024, 3, 1));
        lists.Dividends.Add(dividend);
        lists.CorporateActions.Add(Equalisation(EqualisationIncomeSource.Distribution, 150m, dividend.Date));

        Dividend smaller = Distribution(80m, dividend.Date);
        IncomeChangeResult worse = EqualisationIntegrity.ApplyIncomeChange(lists,
            () => { lists.Dividends.Remove(dividend); lists.Dividends.Add(smaller); }, rejectOverEqualisation: true);
        worse.Applied.ShouldBeFalse();
        lists.Dividends.ShouldHaveSingleItem().ShouldBeSameAs(dividend);

        Dividend larger = Distribution(120m, dividend.Date);
        IncomeChangeResult better = EqualisationIntegrity.ApplyIncomeChange(lists,
            () => { lists.Dividends.Remove(dividend); lists.Dividends.Add(larger); }, rejectOverEqualisation: true);
        better.Applied.ShouldBeTrue();
        lists.Dividends.ShouldHaveSingleItem().ShouldBeSameAs(larger);
    }

    [Fact]
    public void ManuallyEnteredEriDividend_CanBeEqualised()
    {
        TaxEventLists lists = new();
        Dividend manualEri = new()
        {
            AssetName = "VUSA",
            Date = DistributionDate,
            DividendType = DividendType.EXCESS_REPORTABLE_INCOME,
            Proceed = new DescribedMoney(50m, "USD", 0.8m, "Manual ERI")
        };
        lists.Dividends.Add(manualEri);
        lists.CorporateActions.Add(Equalisation(EqualisationIncomeSource.Distribution, 10m, DistributionDate) with { IncomeEventType = nameof(DividendType.EXCESS_REPORTABLE_INCOME) });

        ResolvedIncome resolved = ReportingFundIncomeResolver.Resolve(lists, lists.CorporateActions);

        resolved.Dividends.ShouldHaveSingleItem().Proceed.Amount.Amount.ShouldBe(40m);
    }

    [Fact]
    public void DeletingDistribution_DoesNotMoveItsEqualisationOntoAnotherOnTheSameDay()
    {
        TaxEventLists lists = new();
        DateTime date = new(2024, 3, 1);
        Dividend selected = Distribution(300m, date);
        Dividend other = Distribution(100m, date) with { DividendType = DividendType.DIVIDEND_IN_LIEU };
        lists.Dividends.AddRange([selected, other]);
        FundEqualisation equalisation = Equalisation(EqualisationIncomeSource.Distribution, 200m, date) with { IncomeEventType = nameof(DividendType.DIVIDEND) };
        lists.CorporateActions.Add(equalisation);

        IncomeChangeResult result = EqualisationIntegrity.ApplyIncomeChange(lists, () => lists.Dividends.Remove(selected));

        result.RemovedEqualisations.ShouldHaveSingleItem().ShouldBe(equalisation);
        lists.CorporateActions.ShouldBeEmpty();
    }

    [Fact]
    public void ConvertingDividendToInterest_KeepsItsEqualisation()
    {
        TaxEventLists lists = new();
        Dividend dividend = Distribution(100m, new DateTime(2024, 3, 1));
        lists.Dividends.Add(dividend);
        lists.CorporateActions.Add(Equalisation(EqualisationIncomeSource.Distribution, 10m, dividend.Date) with { IncomeEventType = nameof(DividendType.DIVIDEND) });
        InterestIncome converted = new()
        {
            AssetName = "VUSA",
            Date = dividend.Date,
            InterestType = InterestType.ETFDIVIDEND,
            Amount = dividend.Proceed
        };

        IncomeChangeResult result = EqualisationIntegrity.ApplyIncomeChange(lists,
            () => { lists.Dividends.Remove(dividend); lists.InterestIncomes.Add(converted); });

        result.RemovedEqualisations.ShouldBeEmpty();
        lists.CorporateActions.ShouldHaveSingleItem();
    }

    [Fact]
    public void ApplyIncomeChange_WithRegistry_KeepsEqualisationWhenEditedToAnotherTickerOfTheSameShare()
    {
        const string isin = "IE00B3XXRP09";
        TaxEventLists lists = new();
        Dividend dividend = Distribution(100m, new DateTime(2024, 3, 1)) with { Isin = isin };
        lists.Dividends.Add(dividend);
        // The equalisation form records the share's ISIN from the selected income.
        lists.CorporateActions.Add(Equalisation(EqualisationIncomeSource.Distribution, 10m, dividend.Date) with { IncomeEventType = nameof(DividendType.DIVIDEND), Isin = isin });
        ShareIdentityRegistry registry = new();
        registry.RegisterEvents(lists.AllEvents);
        Dividend renamed = dividend with { AssetName = "VUSA.L" };

        IncomeChangeResult result = EqualisationIntegrity.ApplyIncomeChange(lists,
            () => { lists.Dividends.Remove(dividend); lists.Dividends.Add(renamed); }, shareIdentityRegistry: registry);

        result.RemovedEqualisations.ShouldBeEmpty();
        lists.CorporateActions.ShouldHaveSingleItem();
    }

    [Fact]
    public void LegacyMigration_EriWithoutGeneratedIncome_IsMarkedExcluded()
    {
        // An older session exported after importing with "Load Dividends" unchecked: the ERI is there, its dividend not.
        TaxEventLists lists = new();
        lists.CorporateActions.Add(Eri());

        ReportingFundLegacyMigration.Migrate(lists);

        lists.CorporateActions.ShouldHaveSingleItem().ShouldBeOfType<ExcessReportableIncome>().IsIncomeExcluded.ShouldBeTrue();
        ReportingFundIncomeResolver.Resolve(lists, lists.CorporateActions).Dividends.ShouldBeEmpty();
    }

    [Fact]
    public void JsonImport_CurrentFormat_KeepsManualEriDividendAlongsideEriEntry()
    {
        TaxEventLists session = new();
        session.CorporateActions.Add(Eri());
        session.Dividends.Add(new Dividend
        {
            AssetName = "VUSA",
            Date = DistributionDate,
            DividendType = DividendType.EXCESS_REPORTABLE_INCOME,
            Proceed = new DescribedMoney(5m, "USD", 0.8m, "Manual ERI top-up")
        });
        string json = new ExportTaxEventService(session, new ResidencyStatusRecord(), new ShareIdentityRegistry()).SerialiseTaxEvents();
        InvestmentTaxCalculator.Parser.Json.JsonParseController parser = new(new AssetTypeToLoadSetting(), new ResidencyStatusRecord(), new ShareIdentityRegistry());

        TaxEventLists imported = parser.ParseFile(json);

        imported.Dividends.ShouldHaveSingleItem().Proceed.Amount.Amount.ShouldBe(5m);
        imported.CorporateActions.ShouldHaveSingleItem().ShouldBeOfType<ExcessReportableIncome>().IsIncomeExcluded.ShouldBeFalse();
    }

    [Fact]
    public void JsonImport_FileWithoutFormatVersion_IsConverted()
    {
        TaxEventLists session = new();
        session.CorporateActions.Add(Eri());
        session.Dividends.Add(Eri().CreateIncomeEvent() as Dividend ?? throw new InvalidOperationException());
        string json = new ExportTaxEventService(session, new ResidencyStatusRecord(), new ShareIdentityRegistry()).SerialiseTaxEvents()
            .Replace($"\"FormatVersion\": {ExportImportData.CurrentFormatVersion},", "");
        InvestmentTaxCalculator.Parser.Json.JsonParseController parser = new(new AssetTypeToLoadSetting(), new ResidencyStatusRecord(), new ShareIdentityRegistry());

        TaxEventLists imported = parser.ParseFile(json);

        imported.Dividends.ShouldBeEmpty();
    }
}
