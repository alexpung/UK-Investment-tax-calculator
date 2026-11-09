using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Services;

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
    /// <item>when <paramref name="rejectOverEqualisation"/> is set and the change leaves income further below the
    /// equalisation entered against it than before (e.g. a dividend edited to a smaller amount), undoes the change
    /// and returns an error, since the taxable income would otherwise turn (more) negative;</item>
    /// <item>otherwise deletes the equalisation entries left without their income: those with no income after the
    /// change, and those whose income was removed and that would now fall onto a different income which was already
    /// there (another distribution paid the same day). An equalisation following its income onto a replacement
    /// added by the change (an edit) is kept. Entries that already had no income are left alone.</item>
    /// </list>
    /// </summary>
    /// <param name="taxEventLists">The stored events.</param>
    /// <param name="change">Deletes or edits income in <paramref name="taxEventLists"/>.</param>
    /// <param name="rejectOverEqualisation">Reject changes that leave income smaller than its equalisation.</param>
    /// <param name="shareIdentityRegistry">When given, share identities are refreshed after the change and before
    /// checking, so an edited income recorded under another ticker of the same share still matches.</param>
    public static IncomeChangeResult ApplyIncomeChange(TaxEventLists taxEventLists, Action change, bool rejectOverEqualisation = false,
        ShareIdentityRegistry? shareIdentityRegistry = null)
    {
        List<Dividend> dividendsBefore = [.. taxEventLists.Dividends];
        // Dividend.Proceed can be changed in place, so the amounts are kept as well as the list contents.
        List<(Dividend Dividend, DescribedMoney Proceed)> proceedsBefore = [.. dividendsBefore.Select(d => (d, d.Proceed))];
        List<InterestIncome> interestBefore = [.. taxEventLists.InterestIncomes];
        List<CorporateAction> actionsBefore = [.. taxEventLists.CorporateActions];
        HashSet<TaxEvent> incomeEventsBefore = IncomeEvents(taxEventLists);
        Dictionary<FundEqualisation, TaxEvent> incomeBefore = new(ReferenceEqualityComparer.Instance);
        foreach (FundEqualisation equalisation in taxEventLists.CorporateActions.OfType<FundEqualisation>())
        {
            if (ReportingFundIncomeResolver.FindIncome(equalisation, taxEventLists) is { } income) incomeBefore[equalisation] = income;
        }
        Dictionary<FundEqualisation, decimal> shortfallBefore = ShortfallByEqualisation(taxEventLists);

        change();
        shareIdentityRegistry?.RegisterEvents(taxEventLists.AllEvents);

        if (rejectOverEqualisation)
        {
            List<FundEqualisation> worsened = [.. ShortfallByIncome(taxEventLists)
                .Where(g => g.Shortfall > g.Equalisations.Max(e => shortfallBefore.GetValueOrDefault(e)))
                .SelectMany(g => g.Equalisations)];
            if (worsened.Count > 0)
            {
                Restore(taxEventLists.Dividends, dividendsBefore);
                Restore(taxEventLists.InterestIncomes, interestBefore);
                Restore(taxEventLists.CorporateActions, actionsBefore);
                foreach ((Dividend dividend, DescribedMoney proceed) in proceedsBefore) dividend.Proceed = proceed;
                shareIdentityRegistry?.RegisterEvents(taxEventLists.AllEvents);
                return new IncomeChangeResult(false,
                    "The change was not saved: the income would be smaller than the fund equalisation entered against it (" +
                    string.Join("; ", worsened.Select(e => e.Reason)) + "). Delete or re-enter the equalisation first.", []);
            }
        }

        HashSet<TaxEvent> incomeEventsAfter = IncomeEvents(taxEventLists);
        List<FundEqualisation> stranded = [.. incomeBefore.Where(pair =>
        {
            TaxEvent? incomeAfter = ReportingFundIncomeResolver.FindIncome(pair.Key, taxEventLists);
            if (incomeAfter is null) return true;
            bool ownIncomeRemoved = !incomeEventsAfter.Contains(pair.Value);
            return ownIncomeRemoved && incomeEventsBefore.Contains(incomeAfter);
        }).Select(pair => pair.Key)];
        foreach (FundEqualisation equalisation in stranded)
        {
            taxEventLists.CorporateActions.Remove(equalisation);
        }
        return new IncomeChangeResult(true, null, stranded);
    }

    /// <summary>
    /// Every stored income event an equalisation can reduce, compared by reference.
    /// </summary>
    private static HashSet<TaxEvent> IncomeEvents(TaxEventLists taxEventLists) =>
        new(taxEventLists.Dividends.Cast<TaxEvent>()
            .Concat(taxEventLists.InterestIncomes)
            .Concat(taxEventLists.CorporateActions.OfType<ExcessReportableIncome>()), ReferenceEqualityComparer.Instance);

    /// <summary>
    /// For each income with equalisation, how far (in GBP) the equalisation total exceeds the income, if at all.
    /// </summary>
    private static IEnumerable<(List<FundEqualisation> Equalisations, decimal Shortfall)> ShortfallByIncome(TaxEventLists taxEventLists) =>
        ReportingFundIncomeResolver.GroupEqualisationsByIncome(taxEventLists).Select(g => (g.Value,
            Math.Max(0m, g.Value.Sum(e => e.Amount.BaseCurrencyAmount).Amount - ReportingFundIncomeResolver.IncomeMoneyOf(g.Key).BaseCurrencyAmount.Amount)));

    /// <summary>
    /// The shortfall of the income each equalisation belongs to (see <see cref="ShortfallByIncome"/>).
    /// </summary>
    private static Dictionary<FundEqualisation, decimal> ShortfallByEqualisation(TaxEventLists taxEventLists)
    {
        Dictionary<FundEqualisation, decimal> result = new(ReferenceEqualityComparer.Instance);
        foreach ((List<FundEqualisation> equalisations, decimal shortfall) in ShortfallByIncome(taxEventLists))
        {
            foreach (FundEqualisation equalisation in equalisations) result[equalisation] = shortfall;
        }
        return result;
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
