using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Model.UkTaxModel;

/// <summary>
/// The outcome of <see cref="EqualisationIntegrity.ApplyIncomeChange"/>.
/// </summary>
/// <param name="Applied">False when the change was rejected and undone.</param>
/// <param name="Error">Why the change was rejected, when <paramref name="Applied"/> is false.</param>
/// <param name="RemovedEqualisations">Equalisation entries deleted because the change removed their income.</param>
public record IncomeChangeResult(bool Applied, string? Error, List<FundEqualisation> RemovedEqualisations)
{
    /// <summary>
    /// The warning to show the user about <see cref="RemovedEqualisations"/>, or null if none were removed.
    /// </summary>
    public string? RemovedEqualisationsWarning => RemovedEqualisations.Count == 0
        ? null
        : $"Also deleted {RemovedEqualisations.Count} fund equalisation entr{(RemovedEqualisations.Count == 1 ? "y" : "ies")} " +
          "that reduced the deleted income: " + string.Join("; ", RemovedEqualisations.Select(e => e.Reason)) + ".";
}

/// <summary>
/// Keeps fund equalisation entries consistent with the income they reduce when that income is deleted or edited.
/// </summary>
public static class EqualisationIntegrity
{
    /// <summary>
    /// Applies <paramref name="change"/>, a deletion or edit of an ERI entry or of dividend/interest income, then:
    /// <list type="bullet">
    /// <item>when <paramref name="rejectOverEqualisation"/> is set and the change leaves equalisation entries larger
    /// in total than the income they reduce (e.g. a dividend edited to a smaller amount), undoes the change and
    /// returns an error, since the income would otherwise turn negative;</item>
    /// <item>otherwise deletes the equalisation entries that had income before the change but not after, so none is
    /// left stranded. Entries that already had no income (e.g. imported without their dividends) are left alone.</item>
    /// </list>
    /// </summary>
    public static IncomeChangeResult ApplyIncomeChange(TaxEventLists taxEventLists, Action change, bool rejectOverEqualisation = false)
    {
        List<Dividend> dividendsBefore = [.. taxEventLists.Dividends];
        List<InterestIncome> interestBefore = [.. taxEventLists.InterestIncomes];
        List<CorporateAction> actionsBefore = [.. taxEventLists.CorporateActions];
        List<FundEqualisation> matchedBefore = [.. taxEventLists.CorporateActions.OfType<FundEqualisation>()
            .Where(e => ReportingFundIncomeResolver.HasIncome(e, taxEventLists))];
        HashSet<FundEqualisation> overBefore = ReportingFundIncomeResolver.FindOverEqualised(taxEventLists);

        change();

        if (rejectOverEqualisation)
        {
            List<FundEqualisation> newlyOver = [.. ReportingFundIncomeResolver.FindOverEqualised(taxEventLists).Where(e => !overBefore.Contains(e))];
            if (newlyOver.Count > 0)
            {
                Restore(taxEventLists.Dividends, dividendsBefore);
                Restore(taxEventLists.InterestIncomes, interestBefore);
                Restore(taxEventLists.CorporateActions, actionsBefore);
                return new IncomeChangeResult(false,
                    "The change was not saved: the income would be smaller than the fund equalisation entered against it (" +
                    string.Join("; ", newlyOver.Select(e => e.Reason)) + "). Delete or re-enter the equalisation first.", []);
            }
        }

        List<FundEqualisation> stranded = [.. matchedBefore.Where(e => !ReportingFundIncomeResolver.HasIncome(e, taxEventLists))];
        foreach (FundEqualisation equalisation in stranded)
        {
            taxEventLists.CorporateActions.Remove(equalisation);
        }
        return new IncomeChangeResult(true, null, stranded);
    }

    /// <summary>
    /// Puts the list's contents back to the snapshot taken before a rejected change.
    /// </summary>
    private static void Restore<T>(List<T> list, List<T> snapshot)
    {
        list.Clear();
        list.AddRange(snapshot);
    }
}
