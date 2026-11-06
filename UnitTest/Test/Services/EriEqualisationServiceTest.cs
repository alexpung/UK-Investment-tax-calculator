using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Services;

using Shouldly;

using Xunit;

namespace InvestmentTaxCalculator.UnitTest.Test.Services;

public class EriEqualisationServiceTest
{
    private static readonly DateTime PeriodEnd = new(2023, 12, 31);

    private static (TaxEventLists Lists, EriEqualisationService Service) Create()
    {
        TaxEventLists lists = new();
        return (lists, new EriEqualisationService(lists));
    }

    [Fact]
    public void AddEri_Dividend_AddsCorporateActionAndDividend()
    {
        var (lists, service) = Create();

        ExcessReportableIncome eri = service.AddEri("VUSA", PeriodEnd, 100m, 0.5m, "USD", 0.8m, ExcessReportableIncomeType.DIVIDEND, "IE00B3XXRP09", CountryCode.UnknownRegion);

        lists.CorporateActions.ShouldHaveSingleItem().ShouldBe(eri);
        Dividend dividend = lists.Dividends.ShouldHaveSingleItem();
        dividend.DividendType.ShouldBe(DividendType.EXCESS_REPORTABLE_INCOME);
        dividend.Proceed.Amount.Amount.ShouldBe(50m);
        service.FindIncomeForEri(eri).ShouldBeSameAs(dividend);
    }

    [Fact]
    public void RemoveEri_RemovesCorporateActionAndIncome()
    {
        var (lists, service) = Create();
        ExcessReportableIncome dividendEri = service.AddEri("VUSA", PeriodEnd, 100m, 0.5m, "GBP", 1m, ExcessReportableIncomeType.DIVIDEND, "", CountryCode.UnknownRegion);
        ExcessReportableIncome interestEri = service.AddEri("VUSA", PeriodEnd, 100m, 0.2m, "GBP", 1m, ExcessReportableIncomeType.INTEREST, "", CountryCode.UnknownRegion);

        service.RemoveEri(dividendEri);

        lists.Dividends.ShouldBeEmpty();
        lists.InterestIncomes.ShouldHaveSingleItem();
        lists.CorporateActions.ShouldHaveSingleItem().ShouldBe(interestEri);

        service.RemoveEri(interestEri);

        lists.InterestIncomes.ShouldBeEmpty();
        lists.CorporateActions.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveEri_DoesNotRemoveUnrelatedDividendOnSameDate()
    {
        var (lists, service) = Create();
        ExcessReportableIncome eri = service.AddEri("VUSA", PeriodEnd, 100m, 0.5m, "GBP", 1m, ExcessReportableIncomeType.DIVIDEND, "", CountryCode.UnknownRegion);
        Dividend regular = new()
        {
            AssetName = "VUSA",
            Date = eri.Date,
            DividendType = DividendType.DIVIDEND,
            Proceed = new DescribedMoney(20m, "GBP", 1m, "Regular dividend")
        };
        lists.Dividends.Add(regular);

        service.RemoveEri(eri);

        lists.Dividends.ShouldHaveSingleItem().ShouldBeSameAs(regular);
    }

    [Fact]
    public void UpdateEri_ReplacesEntryAndIncome()
    {
        var (lists, service) = Create();
        ExcessReportableIncome eri = service.AddEri("VUSA", PeriodEnd, 100m, 0.5m, "GBP", 1m, ExcessReportableIncomeType.DIVIDEND, "", CountryCode.UnknownRegion);

        ExcessReportableIncome updated = service.UpdateEri(eri, "VUSA", PeriodEnd, 100m, 0.3m, "GBP", 1m, ExcessReportableIncomeType.INTEREST, "", CountryCode.UnknownRegion);

        lists.CorporateActions.ShouldHaveSingleItem().ShouldBe(updated);
        lists.Dividends.ShouldBeEmpty();
        lists.InterestIncomes.ShouldHaveSingleItem().Amount.Amount.Amount.ShouldBe(30m);
    }

    [Fact]
    public void RemoveEqualisation_RestoresDividendAmountAndDescription()
    {
        var (lists, service) = Create();
        Dividend dividend = new()
        {
            AssetName = "VUSA",
            Date = new DateTime(2024, 3, 1),
            DividendType = DividendType.DIVIDEND,
            Proceed = new DescribedMoney(100m, "USD", 0.8m, "Dividend VUSA")
        };
        lists.Dividends.Add(dividend);

        FundEqualisation equalisation = service.AddEqualisation("VUSA", dividend, 12.34m);

        dividend.Proceed.Amount.Amount.ShouldBe(87.66m);
        service.FindIncomeForEqualisation(equalisation).ShouldBeSameAs(dividend);

        service.RemoveEqualisation(equalisation).ShouldBeSameAs(dividend);

        lists.CorporateActions.ShouldBeEmpty();
        dividend.Proceed.Amount.Amount.ShouldBe(100m);
        dividend.Proceed.Amount.Currency.ShouldBe("USD");
        dividend.Proceed.FxRate.ShouldBe(0.8m);
        dividend.Proceed.Description.ShouldBe("Dividend VUSA");
    }

    [Fact]
    public void RemoveEqualisation_RestoresInterestIncome()
    {
        var (lists, service) = Create();
        ExcessReportableIncome eri = service.AddEri("VUSA", PeriodEnd, 100m, 0.5m, "GBP", 1m, ExcessReportableIncomeType.INTEREST, "", CountryCode.UnknownRegion);
        InterestIncome income = lists.InterestIncomes.ShouldHaveSingleItem();

        FundEqualisation equalisation = service.AddEqualisation("VUSA", income, 10m);
        equalisation.ReportingPeriodEndDate.ShouldBe(PeriodEnd);
        lists.InterestIncomes.ShouldHaveSingleItem().Amount.Amount.Amount.ShouldBe(40m);
        service.GetEqualisationsLinkedToEri(eri).ShouldHaveSingleItem().ShouldBe(equalisation);

        service.RemoveEqualisation(equalisation);

        InterestIncome restored = lists.InterestIncomes.ShouldHaveSingleItem();
        restored.Amount.Amount.Amount.ShouldBe(50m);
        restored.Amount.Description.ShouldBe(income.Amount.Description);
        service.GetEqualisationsLinkedToEri(eri).ShouldBeEmpty();
    }

    [Fact]
    public void RemoveEri_WithLinkedEqualisation_IsRefusedAndNothingChanges()
    {
        var (lists, service) = Create();
        ExcessReportableIncome eri = service.AddEri("VUSA", PeriodEnd, 100m, 0.5m, "GBP", 1m, ExcessReportableIncomeType.DIVIDEND, "", CountryCode.UnknownRegion);
        service.AddEqualisation("VUSA", lists.Dividends.Single(), 10m);

        Should.Throw<InvalidOperationException>(() => service.RemoveEri(eri));

        lists.CorporateActions.Count.ShouldBe(2);
        lists.Dividends.ShouldHaveSingleItem().Proceed.Amount.Amount.ShouldBe(40m);
    }

    [Fact]
    public void UpdateEqualisation_ChangesAmountOnSameEvent()
    {
        var (lists, service) = Create();
        ExcessReportableIncome eri = service.AddEri("VUSA", PeriodEnd, 100m, 0.5m, "GBP", 1m, ExcessReportableIncomeType.INTEREST, "", CountryCode.UnknownRegion);
        FundEqualisation equalisation = service.AddEqualisation("VUSA", lists.InterestIncomes.Single(), 10m);
        // The UI passes the event it found for the entry being edited.
        TaxEvent linked = service.FindIncomeForEqualisation(equalisation)!;

        // 45 only fits once the original 10 is added back (50 available).
        FundEqualisation updated = service.UpdateEqualisation(equalisation, "VUSA", linked, 45m);

        lists.CorporateActions.OfType<FundEqualisation>().ShouldHaveSingleItem().ShouldBe(updated);
        lists.InterestIncomes.ShouldHaveSingleItem().Amount.Amount.Amount.ShouldBe(5m);
        service.GetEqualisationsLinkedToEri(eri).ShouldHaveSingleItem().ShouldBe(updated);
    }

    [Fact]
    public void UpdateEqualisation_InvalidAmount_KeepsOriginal()
    {
        var (lists, service) = Create();
        Dividend dividend = new()
        {
            AssetName = "VUSA",
            Date = new DateTime(2024, 3, 1),
            DividendType = DividendType.DIVIDEND,
            Proceed = new DescribedMoney(100m, "GBP", 1m, "Dividend VUSA")
        };
        lists.Dividends.Add(dividend);
        FundEqualisation equalisation = service.AddEqualisation("VUSA", dividend, 10m);

        Should.Throw<InvalidOperationException>(() => service.UpdateEqualisation(equalisation, "VUSA", dividend, 150m));

        FundEqualisation kept = lists.CorporateActions.OfType<FundEqualisation>().ShouldHaveSingleItem();
        kept.Amount.Amount.Amount.ShouldBe(10m);
        dividend.Proceed.Amount.Amount.ShouldBe(90m);
    }

    [Fact]
    public void AddEqualisation_ExceedingIncome_IsRefused()
    {
        var (lists, service) = Create();
        Dividend dividend = new()
        {
            AssetName = "VUSA",
            Date = new DateTime(2024, 3, 1),
            DividendType = DividendType.DIVIDEND,
            Proceed = new DescribedMoney(10m, "GBP", 1m)
        };
        lists.Dividends.Add(dividend);

        Should.Throw<InvalidOperationException>(() => service.AddEqualisation("VUSA", dividend, 11m));

        lists.CorporateActions.ShouldBeEmpty();
        dividend.Proceed.Amount.Amount.ShouldBe(10m);
    }
}
