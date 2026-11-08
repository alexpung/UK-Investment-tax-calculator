using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Services;

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
    /// <summary>
    /// Converts <paramref name="taxEventLists"/> (one imported session file) in place.
    /// </summary>
    /// <param name="taxEventLists">The events read from the file.</param>
    /// <param name="manualLinks">The share links saved in the same file, so an ERI or equalisation entry and its
    /// income are matched even when they were recorded under different tickers of the same share.</param>
    public static void Migrate(TaxEventLists taxEventLists, IEnumerable<ShareIdentityLink>? manualLinks = null)
    {
        // Resolve share identities among the file's own events, without touching the app-wide registry (which is
        // rebuilt from all events once the import is merged).
        ShareIdentityRegistry registry = new();
        registry.ImportManualLinks(manualLinks ?? []);
        registry.RegisterEvents(taxEventLists.AllEvents);

        List<ExcessReportableIncome> eris = [.. taxEventLists.CorporateActions.OfType<ExcessReportableIncome>()];
        List<FundEqualisation> legacyEqualisations = [.. taxEventLists.CorporateActions.OfType<FundEqualisation>().Where(e => e.IncomeSource is null)];

        foreach (FundEqualisation legacy in legacyEqualisations)
        {
            EqualisationIncomeSource source = IsEriEqualisation(legacy) ? EqualisationIncomeSource.ExcessReportableIncome : EqualisationIncomeSource.Distribution;
            FundEqualisation migrated = legacy with { IncomeSource = source };
            TaxEvent? income = source == EqualisationIncomeSource.Distribution
                ? RestoreDistribution(taxEventLists, migrated)
                : ReportingFundIncomeResolver.FindExcessReportableIncome(migrated, eris);
            if (income is not null)
            {
                // Record which income it reduces, so it stays on that income if another is paid on the same date.
                migrated = migrated with { IncomeEventType = ReportingFundIncomeResolver.IncomeEventTypeOf(income) };
            }
            int index = taxEventLists.CorporateActions.IndexOf(legacy);
            taxEventLists.CorporateActions[index] = migrated;
        }

        // The ERI income is now derived from the ERI entry at calculation time.
        taxEventLists.Dividends.RemoveAll(d => d.DividendType == DividendType.EXCESS_REPORTABLE_INCOME && eris.Any(eri => IsGeneratedBy(d, eri)));
        taxEventLists.InterestIncomes.RemoveAll(i => i.InterestType == InterestType.EXCESSREPORTABLEINCOME && eris.Any(eri => IsGeneratedBy(i, eri)));
    }

    /// <summary>
    /// Whether the income event is the one older versions generated for the ERI entry: same share, same date and
    /// the same kind of income (dividend or interest).
    /// </summary>
    private static bool IsGeneratedBy(TaxEvent income, ExcessReportableIncome eri) =>
        (income.IsSameAsset(eri.AssetName) || eri.IsSameAsset(income.AssetName)) && income.Date == eri.Date
        && (eri.IncomeType == ExcessReportableIncomeType.DIVIDEND) == (income is Dividend);

    /// <summary>
    /// Whether a legacy equalisation entry reduced ERI income. Older versions set the reporting period end only on
    /// equalisation of ERI income, and described the event reduced by its income type.
    /// </summary>
    private static bool IsEriEqualisation(FundEqualisation equalisation) =>
        equalisation.ReportingPeriodEndDate is not null
        || (equalisation.RelatedEventDescription?.StartsWith(DividendType.EXCESS_REPORTABLE_INCOME.GetDescription(), StringComparison.Ordinal) ?? false)
        || (equalisation.RelatedEventDescription?.StartsWith(InterestType.EXCESSREPORTABLEINCOME.GetDescription(), StringComparison.Ordinal) ?? false);

    /// <summary>
    /// Adds a legacy distribution equalisation's amount back to the distribution it was taken off, and returns that
    /// distribution (or null if it is not in the file).
    /// </summary>
    private static TaxEvent? RestoreDistribution(TaxEventLists taxEventLists, FundEqualisation equalisation)
    {
        TaxEvent? income = ReportingFundIncomeResolver.FindDistribution(equalisation, taxEventLists.Dividends, taxEventLists.InterestIncomes);
        if (income is null) return null;
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
        return income;
    }

    /// <summary>
    /// The reduced income amount with the equalisation added back and its "(Reduced by equalisation ...)" note removed.
    /// </summary>
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
