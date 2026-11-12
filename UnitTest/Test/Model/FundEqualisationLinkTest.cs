using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Parser.Json;
using InvestmentTaxCalculator.Services;
using InvestmentTaxCalculator.ViewModel;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using Xunit;

namespace UnitTest.Test.Model;

/// <summary>
/// The two-way link between a fund equalisation entry and its income, the income's own checks, and how ERI income
/// and equalisation are applied at calculation time without changing stored income.
/// </summary>
public class FundEqualisationLinkTest
{
    private static readonly DateTime PeriodEnd = new(2023, 12, 31);
    private static readonly DateTime DistributionDate = new(2024, 6, 30);

    private static Dividend Dividend(decimal amount, string asset = "VUSA") => new()
    {
        AssetName = asset,
        Date = new DateTime(2024, 3, 1),
        DividendType = DividendType.DIVIDEND,
        Proceed = new DescribedMoney(amount, "USD", 0.8m, "Dividend")
    };

    private static InterestIncome Interest(decimal amount) => new()
    {
        AssetName = "VUSA",
        Date = new DateTime(2024, 3, 1),
        InterestType = InterestType.ETFDIVIDEND,
        Amount = new DescribedMoney(amount, "USD", 0.8m, "Bond fund distribution")
    };

    private static ExcessReportableIncome Eri(decimal amount = 50m) => new()
    {
        AssetName = "VUSA",
        Date = DistributionDate,
        ReportingPeriodEndDate = PeriodEnd,
        IncomeType = ExcessReportableIncomeType.DIVIDEND,
        Amount = new DescribedMoney(amount, "USD", 0.8m, "ERI")
    };

    /// <summary>
    /// An equalisation in USD at 0.8, like the incomes above. Its asset and date are placeholders: once linked it
    /// takes both from its income.
    /// </summary>
    private static FundEqualisation Equalisation(decimal amount) => new()
    {
        AssetName = "placeholder",
        Date = DateTime.MinValue,
        Amount = new DescribedMoney(amount, "USD", 0.8m, "Equalisation"),
        RelatedEventDescription = "Dividend"
    };

    [Fact]
    public void AddEqualisation_LinksBothWays_AndFollowsTheIncome()
    {
        TaxEventLists lists = new();
        Dividend dividend = Dividend(100m);
        lists.Dividends.Add(dividend);
        FundEqualisation equalisation = Equalisation(10m);

        lists.AddEqualisation(equalisation, dividend);

        equalisation.LinkedIncome.ShouldBeSameAs(dividend);
        dividend.EqualisationLinks.Equalisations.ShouldHaveSingleItem().ShouldBeSameAs(equalisation);
        equalisation.IncomeId.ShouldBe(dividend.IncomeId);
        equalisation.AssetName.ShouldBe("VUSA");
        equalisation.Date.ShouldBe(dividend.Date);

        // Editing the income's asset or date moves the equalisation with it.
        dividend.AssetName = "VUSA.L";
        dividend.Date = new DateTime(2024, 3, 2);
        equalisation.AssetName.ShouldBe("VUSA.L");
        equalisation.Date.ShouldBe(new DateTime(2024, 3, 2));
    }

    [Fact]
    public void AddEqualisation_ExceedingIncome_IsRefusedAndNothingIsStored()
    {
        TaxEventLists lists = new();
        Dividend dividend = Dividend(100m);
        lists.Dividends.Add(dividend);
        lists.AddEqualisation(Equalisation(60m), dividend);

        Should.Throw<InvalidOperationException>(() => lists.AddEqualisation(Equalisation(50m), dividend));

        lists.CorporateActions.ShouldHaveSingleItem();
        dividend.EqualisationLinks.Equalisations.ShouldHaveSingleItem();
    }

    [Fact]
    public void DividendAmount_CannotBeSetBelowItsEqualisation()
    {
        Dividend dividend = Dividend(100m);
        Equalisation(50m).LinkTo(dividend);
        DescribedMoney original = dividend.Proceed;

        Should.Throw<InvalidOperationException>(() => dividend.Proceed = new DescribedMoney(40m, "USD", 0.8m, "Edited"));
        dividend.Proceed.ShouldBeSameAs(original);

        dividend.Proceed = new DescribedMoney(50m, "USD", 0.8m, "Edited");
        dividend.Proceed.Amount.Amount.ShouldBe(50m);
    }

    [Fact]
    public void InterestAmount_CannotBeSetBelowItsEqualisation()
    {
        InterestIncome interest = Interest(100m);
        Equalisation(50m).LinkTo(interest);

        Should.Throw<InvalidOperationException>(() => interest.Amount = new DescribedMoney(40m, "USD", 0.8m, "Edited"));
        interest.Amount.Amount.Amount.ShouldBe(100m);
    }

    [Fact]
    public void EqualisedDividend_CannotBecomeWithholdingTax()
    {
        Dividend dividend = Dividend(100m);
        Equalisation(10m).LinkTo(dividend);

        Should.Throw<InvalidOperationException>(() => dividend.DividendType = DividendType.WITHHOLDING);
        dividend.DividendType.ShouldBe(DividendType.DIVIDEND);
    }

    [Fact]
    public void RemoveIncome_AlsoRemovesItsEqualisations()
    {
        TaxEventLists lists = new();
        Dividend dividend = Dividend(100m);
        Dividend other = Dividend(100m);
        lists.Dividends.AddRange([dividend, other]);
        FundEqualisation equalisation = Equalisation(10m);
        FundEqualisation otherEqualisation = Equalisation(10m);
        lists.AddEqualisation(equalisation, dividend);
        lists.AddEqualisation(otherEqualisation, other);

        List<FundEqualisation> removed = lists.RemoveIncome(dividend);

        removed.ShouldHaveSingleItem().ShouldBeSameAs(equalisation);
        equalisation.LinkedIncome.ShouldBeNull();
        lists.Dividends.ShouldHaveSingleItem().ShouldBeSameAs(other);
        lists.CorporateActions.ShouldHaveSingleItem().ShouldBeSameAs(otherEqualisation);
        FundEqualisation.DescribeRemovedWithIncome(removed).ShouldNotBeNull().ShouldContain("1 fund equalisation entry");
    }

    [Fact]
    public void RemoveCorporateAction_EriTakesItsEqualisations_EqualisationIsDetached()
    {
        TaxEventLists lists = new();
        ExcessReportableIncome eri = Eri();
        lists.CorporateActions.Add(eri);
        FundEqualisation first = Equalisation(5m);
        FundEqualisation second = Equalisation(5m);
        lists.AddEqualisation(first, eri);
        lists.AddEqualisation(second, eri);

        lists.RemoveCorporateAction(second).ShouldBeEmpty();
        eri.EqualisationLinks.Equalisations.ShouldHaveSingleItem().ShouldBeSameAs(first);

        lists.RemoveCorporateAction(eri).ShouldHaveSingleItem().ShouldBeSameAs(first);
        lists.CorporateActions.ShouldBeEmpty();
    }

    [Fact]
    public void LinkTo_MovesTheEqualisationToAnotherIncome()
    {
        // As when a fund dividend is converted to interest income.
        Dividend dividend = Dividend(100m);
        InterestIncome interest = Interest(100m);
        FundEqualisation equalisation = Equalisation(10m);
        equalisation.LinkTo(dividend);

        equalisation.LinkTo(interest);

        dividend.EqualisationLinks.Equalisations.ShouldBeEmpty();
        interest.EqualisationLinks.Equalisations.ShouldHaveSingleItem().ShouldBeSameAs(equalisation);
        equalisation.IncomeId.ShouldBe(interest.IncomeId);
    }

    [Fact]
    public void ConvertDividendsToIncome_KeepsTheEqualisationOnTheConvertedIncome()
    {
        TaxEventLists lists = new();
        Dividend dividend = Dividend(100m);
        lists.Dividends.Add(dividend);
        FundEqualisation equalisation = Equalisation(10m);
        lists.AddEqualisation(equalisation, dividend);

        new DividendToIncomeConvertViewModel(lists).ConvertDividendsToIncome(["VUSA"]);

        InterestIncome converted = lists.InterestIncomes.ShouldHaveSingleItem();
        equalisation.LinkedIncome.ShouldBeSameAs(converted);
        ReportingFundIncomeResolver.Resolve(lists).InterestIncomes.Single().Amount.Amount.Amount.ShouldBe(90m);
    }

    [Fact]
    public void Resolve_AddsEriIncomeAndDeductsEqualisation_WithoutChangingStoredIncome()
    {
        TaxEventLists lists = new();
        Dividend dividend = Dividend(100m);
        lists.Dividends.Add(dividend);
        ExcessReportableIncome eri = Eri(50m);
        lists.CorporateActions.Add(eri);
        lists.AddEqualisation(Equalisation(12.5m), dividend);
        lists.AddEqualisation(Equalisation(10m), eri);

        // Run twice: nothing accumulates.
        ReportingFundIncomeResolver.Resolve(lists);
        ResolvedIncome resolved = ReportingFundIncomeResolver.Resolve(lists);

        resolved.Dividends.Count.ShouldBe(2);
        Dividend reduced = resolved.Dividends.Single(d => d.DividendType == DividendType.DIVIDEND);
        reduced.Proceed.Amount.Amount.ShouldBe(87.5m);
        reduced.Id.ShouldBe(dividend.Id);
        reduced.ShouldNotBeSameAs(dividend);
        reduced.EqualisationLinks.Equalisations.ShouldBeEmpty();
        resolved.Dividends.Single(d => d.DividendType == DividendType.EXCESS_REPORTABLE_INCOME).Proceed.Amount.Amount.ShouldBe(40m);

        lists.Dividends.ShouldHaveSingleItem().ShouldBeSameAs(dividend);
        dividend.Proceed.Amount.Amount.ShouldBe(100m);
        dividend.EqualisationLinks.Equalisations.Count.ShouldBe(1);
        eri.Amount.Amount.Amount.ShouldBe(50m);
    }

    [Fact]
    public void Resolve_DescribesTheDeduction_WithOrWithoutAnExistingDescription()
    {
        TaxEventLists lists = new();
        Dividend described = Dividend(100m);
        Dividend undescribed = Dividend(100m) with { Proceed = new DescribedMoney(100m, "USD", 0.8m) };
        lists.Dividends.AddRange([described, undescribed]);
        lists.AddEqualisation(Equalisation(10m), described);
        lists.AddEqualisation(Equalisation(10m), undescribed);

        ResolvedIncome resolved = ReportingFundIncomeResolver.Resolve(lists);

        resolved.Dividends[0].Proceed.Description.ShouldBe("Dividend (Reduced by equalisation of £8.00)");
        resolved.Dividends[1].Proceed.Description.ShouldBe("Reduced by equalisation of £8.00");
    }

    [Fact]
    public void DividendCalculator_RunRepeatedly_GivesTheSameIncome()
    {
        TaxEventLists lists = new();
        Dividend dividend = Dividend(100m);
        lists.Dividends.Add(dividend);
        ExcessReportableIncome eri = Eri(50m);
        lists.CorporateActions.Add(eri);
        lists.AddEqualisation(Equalisation(10m), dividend);
        lists.AddEqualisation(Equalisation(10m), eri);
        ToastService toastService = Substitute.For<ToastService>(Substitute.For<ILogger<ToastService>>());
        UkDividendCalculator calculator = new(lists, new UKTaxYear(), toastService, new ResidencyStatusRecord());

        List<List<DividendSummary>> runs = [calculator.CalculateTax(), calculator.CalculateTax(), calculator.CalculateTax()];

        // ERI 50 USD - 10 USD, dividend 100 USD - 10 USD, at 0.8.
        foreach (List<DividendSummary> summaries in runs)
        {
            summaries.Sum(s => s.TotalTaxableDividend).Amount.ShouldBe(104m);
            summaries.Sum(s => s.TotalExcessReportableIncomeDividend).Amount.ShouldBe(32m);
        }
        lists.Dividends.ShouldHaveSingleItem().Proceed.Amount.Amount.ShouldBe(100m);
    }

    [Fact]
    public void EriEqualisation_UsesTheEriReportingPeriod()
    {
        ExcessReportableIncome eri = Eri();
        FundEqualisation equalisation = Equalisation(10m);
        equalisation.LinkTo(eri);

        equalisation.ReportingPeriodEndDate.ShouldBe(DateOnly.FromDateTime(PeriodEnd));
        equalisation.Date.ShouldBe(DistributionDate);
    }

    [Fact]
    public void Links_DoNotAffectEquality_AndCopiesAreNotLinked()
    {
        Dividend dividend = Dividend(100m);
        int hashBefore = dividend.GetHashCode();
        Equalisation(10m).LinkTo(dividend);

        dividend.GetHashCode().ShouldBe(hashBefore);
        Dividend copy = dividend with { };
        copy.EqualisationLinks.Equalisations.ShouldBeEmpty();
        copy.ShouldBe(dividend);
        ExcessReportableIncome eri = Eri();
        ExcessReportableIncome eriCopy = eri with { };
        Equalisation(10m).LinkTo(eri);
        eriCopy.EqualisationLinks.Equalisations.ShouldBeEmpty();
        eriCopy.ShouldBe(eri);
        // An unlinked copy may be set below the original's equalisation.
        Should.NotThrow(() => copy.Proceed = new DescribedMoney(1m, "USD", 0.8m, "Copy"));
        dividend.EqualisationLinks.Equalisations.ShouldHaveSingleItem();
    }

    private static string Export(TaxEventLists lists) =>
        new ExportTaxEventService(lists, new ResidencyStatusRecord(), new ShareIdentityRegistry()).SerialiseTaxEvents();

    private static TaxEventLists Import(string json, AssetTypeToLoadSetting? setting = null) =>
        new JsonParseController(setting ?? new AssetTypeToLoadSetting(), new ResidencyStatusRecord(), new ShareIdentityRegistry()).ParseFile(json);

    [Fact]
    public void ExportImport_RestoresTheTwoWayLinks()
    {
        TaxEventLists lists = new();
        Dividend dividend = Dividend(100m);
        lists.Dividends.Add(dividend);
        ExcessReportableIncome eri = Eri();
        lists.CorporateActions.Add(eri);
        lists.AddEqualisation(Equalisation(10m), dividend);
        lists.AddEqualisation(Equalisation(5m), eri);

        TaxEventLists imported = Import(Export(lists));

        Dividend importedDividend = imported.Dividends.ShouldHaveSingleItem();
        ExcessReportableIncome importedEri = imported.CorporateActions.OfType<ExcessReportableIncome>().Single();
        List<FundEqualisation> equalisations = [.. imported.CorporateActions.OfType<FundEqualisation>()];
        equalisations.Count.ShouldBe(2);
        equalisations.Single(e => e.Amount.Amount.Amount == 10m).LinkedIncome.ShouldBeSameAs(importedDividend);
        equalisations.Single(e => e.Amount.Amount.Amount == 5m).LinkedIncome.ShouldBeSameAs(importedEri);
        importedDividend.EqualisationLinks.Equalisations.ShouldHaveSingleItem();
        importedEri.EqualisationLinks.Equalisations.ShouldHaveSingleItem();
    }

    [Fact]
    public void Import_WithDividendsNotLoaded_DropsTheirEqualisations()
    {
        TaxEventLists lists = new();
        Dividend dividend = Dividend(100m);
        lists.Dividends.Add(dividend);
        lists.AddEqualisation(Equalisation(10m), dividend);

        TaxEventLists imported = Import(Export(lists), new AssetTypeToLoadSetting { LoadDividends = false });
        TaxEventLists session = new();
        session.AddData(imported, skipDuplicates: false);

        List<FundEqualisation> dropped = session.RemoveUnlinkedEqualisations();

        dropped.ShouldHaveSingleItem().Amount.Amount.Amount.ShouldBe(10m);
        session.Dividends.ShouldBeEmpty();
        session.CorporateActions.ShouldBeEmpty();
    }

    [Fact]
    public void AddData_SkippingDuplicates_LinksNewEqualisationToTheExistingIncome()
    {
        // The session is exported, an equalisation is added to the export's copy, and that file is imported back:
        // the dividend is skipped as a duplicate, and the new equalisation attaches to the dividend already here.
        TaxEventLists lists = new();
        Dividend dividend = Dividend(100m);
        lists.Dividends.Add(dividend);
        TaxEventLists later = Import(Export(lists));
        later.AddEqualisation(Equalisation(10m), later.Dividends.Single());
        string json = Export(later);

        lists.AddData(Import(json), skipDuplicates: true);
        lists.RemoveUnlinkedEqualisations().ShouldBeEmpty();

        lists.Dividends.ShouldHaveSingleItem().ShouldBeSameAs(dividend);
        lists.CorporateActions.ShouldHaveSingleItem();
        dividend.EqualisationLinks.Equalisations.ShouldHaveSingleItem();
    }

    [Fact]
    public void AddData_WithoutSkippingDuplicates_KeepsEachCopyLinkedToItsOwnIncome()
    {
        TaxEventLists lists = new();
        Dividend dividend = Dividend(100m);
        lists.Dividends.Add(dividend);
        lists.AddEqualisation(Equalisation(10m), dividend);
        string json = Export(lists);

        lists.AddData(Import(json), skipDuplicates: false);
        lists.RemoveUnlinkedEqualisations().ShouldBeEmpty();

        lists.Dividends.Count.ShouldBe(2);
        lists.Dividends.Select(d => d.IncomeId).Distinct().Count().ShouldBe(2);
        lists.Dividends.ShouldAllBe(d => d.EqualisationLinks.Equalisations.Count == 1);
        // A further round trip keeps them apart.
        TaxEventLists again = Import(Export(lists));
        again.Dividends.ShouldAllBe(d => d.EqualisationLinks.Equalisations.Count == 1);
    }

    [Fact]
    public void AddEqualisation_ToIncomeNotInTheLists_IsRefused()
    {
        TaxEventLists lists = new();

        Should.Throw<InvalidOperationException>(() => lists.AddEqualisation(Equalisation(10m), Dividend(100m)));

        lists.CorporateActions.ShouldBeEmpty();
    }

    [Fact]
    public void IncomeTypesThatCannotBeEqualised_AreRefusedByTheModel()
    {
        Dividend withholding = Dividend(100m);
        withholding.DividendType = DividendType.WITHHOLDING;
        Should.Throw<InvalidOperationException>(() => Equalisation(10m).LinkTo(withholding));

        InterestIncome interest = Interest(100m);
        Equalisation(10m).LinkTo(interest);
        Should.Throw<InvalidOperationException>(() => interest.InterestType = InterestType.INTERESTEXPENSE);
        interest.InterestType.ShouldBe(InterestType.ETFDIVIDEND);
    }

    [Fact]
    public void RemovedEqualisation_KeepsTheAssetAndDateItsIncomeHadWhenRemoved()
    {
        TaxEventLists lists = new();
        Dividend dividend = Dividend(100m);
        lists.Dividends.Add(dividend);
        lists.AddEqualisation(Equalisation(10m), dividend);
        dividend.AssetName = "VUSA.L";
        dividend.Date = new DateTime(2025, 1, 2);

        FundEqualisation removed = lists.RemoveIncome(dividend).ShouldHaveSingleItem();

        removed.AssetName.ShouldBe("VUSA.L");
        removed.Date.ShouldBe(new DateTime(2025, 1, 2));
    }

    [Fact]
    public void RemoveUnlinkedEqualisations_RemovesAndReturnsEntriesWithoutIncome()
    {
        // As in a session saved by a version that did not link equalisation to its income.
        TaxEventLists lists = new();
        lists.Dividends.Add(Dividend(100m));
        FundEqualisation noIncomeId = Equalisation(10m);
        lists.CorporateActions.Add(noIncomeId);

        lists.RemoveUnlinkedEqualisations().ShouldHaveSingleItem().ShouldBeSameAs(noIncomeId);

        lists.CorporateActions.ShouldBeEmpty();
    }
}
