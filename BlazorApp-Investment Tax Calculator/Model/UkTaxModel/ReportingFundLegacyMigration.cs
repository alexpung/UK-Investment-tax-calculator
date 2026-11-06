using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Model.UkTaxModel;

/// <summary>
/// Converts ERI and fund equalisation data saved by versions that applied the entries to other events when they
/// were entered, into the current form where they are applied only at calculation time
/// (see <see cref="ReportingFundIncomeResolver"/>). Older sessions stored:
/// <list type="bullet">
/// <item>a generated ERI dividend/interest event alongside each ERI entry, and</item>
/// <item>income events whose amount had already been reduced by an equalisation entry.</item>
/// </list>
/// Left as they are, both would be counted again by the calculation, so the generated ERI income is dropped and
/// the reduced distributions are restored. Run on every imported session file; files saved by the current version contain neither, so it changes
/// nothing for them. Equalisation entries without an <see cref="FundEqualisation.IncomeSource"/> are old entries.
/// </summary>
public static class ReportingFundLegacyMigration
{
    public static void Migrate(TaxEventLists taxEventLists)
    {
        List<ExcessReportableIncome> eris = [.. taxEventLists.CorporateActions.OfType<ExcessReportableIncome>()];
        List<FundEqualisation> legacyEqualisations = [.. taxEventLists.CorporateActions.OfType<FundEqualisation>().Where(e => e.IncomeSource is null)];

        foreach (FundEqualisation legacy in legacyEqualisations)
        {
            EqualisationIncomeSource source = IsEriEqualisation(legacy) ? EqualisationIncomeSource.ExcessReportableIncome : EqualisationIncomeSource.Distribution;
            FundEqualisation migrated = legacy with { IncomeSource = source };
            int index = taxEventLists.CorporateActions.IndexOf(legacy);
            taxEventLists.CorporateActions[index] = migrated;

            if (source == EqualisationIncomeSource.Distribution)
            {
                RestoreDistribution(taxEventLists, migrated);
            }
        }

        // The ERI income is now derived from the ERI entry at calculation time.
        taxEventLists.Dividends.RemoveAll(d => d.DividendType == DividendType.EXCESS_REPORTABLE_INCOME && eris.Any(eri => IsGeneratedBy(d, eri)));
        taxEventLists.InterestIncomes.RemoveAll(i => i.InterestType == InterestType.EXCESSREPORTABLEINCOME && eris.Any(eri => IsGeneratedBy(i, eri)));
    }

    private static bool IsGeneratedBy(TaxEvent income, ExcessReportableIncome eri) =>
        income.AssetName == eri.AssetName && income.Date == eri.Date
        && (eri.IncomeType == ExcessReportableIncomeType.DIVIDEND) == (income is Dividend);

    private static bool IsEriEqualisation(FundEqualisation equalisation) =>
        // Older versions set the reporting period end only on equalisation of ERI income, and described the event
        // reduced by its income type.
        equalisation.ReportingPeriodEndDate is not null
        || (equalisation.RelatedEventDescription?.StartsWith(DividendType.EXCESS_REPORTABLE_INCOME.GetDescription(), StringComparison.Ordinal) ?? false)
        || (equalisation.RelatedEventDescription?.StartsWith(InterestType.EXCESSREPORTABLEINCOME.GetDescription(), StringComparison.Ordinal) ?? false);

    private static void RestoreDistribution(TaxEventLists taxEventLists, FundEqualisation equalisation)
    {
        TaxEvent? income = ReportingFundIncomeResolver.FindDistribution(equalisation, taxEventLists.Dividends, taxEventLists.InterestIncomes);
        if (income is null) return;
        WrappedMoney reductionBase = equalisation.Amount.BaseCurrencyAmount;
        string note = $" (Reduced by equalisation of {reductionBase})";
        switch (income)
        {
            case Dividend dividend:
                dividend.Proceed = Restore(dividend.Proceed, reductionBase, note);
                break;
            case InterestIncome interest:
                int index = taxEventLists.InterestIncomes.IndexOf(interest);
                taxEventLists.InterestIncomes[index] = interest with { Amount = Restore(interest.Amount, reductionBase, note) };
                break;
        }
    }

    private static DescribedMoney Restore(DescribedMoney reduced, WrappedMoney reductionBase, string note)
    {
        int noteIndex = reduced.Description.LastIndexOf(note, StringComparison.Ordinal);
        return new DescribedMoney
        {
            Amount = reduced.Amount + new WrappedMoney(reductionBase.Amount / reduced.FxRate, reduced.Amount.Currency),
            FxRate = reduced.FxRate,
            Description = noteIndex >= 0 ? reduced.Description.Remove(noteIndex, note.Length) : reduced.Description
        };
    }
}
