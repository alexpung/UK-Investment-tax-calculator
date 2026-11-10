using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Model.UkTaxModel;

/// <summary>
/// The dividend and interest income to tax once reporting fund entries are taken into account.
/// </summary>
/// <param name="Dividends">Stored dividends plus ERI dividends, with equalisation deducted.</param>
/// <param name="InterestIncomes">Stored interest plus ERI interest, with equalisation deducted.</param>
public record ResolvedIncome(List<Dividend> Dividends, List<InterestIncome> InterestIncomes);

/// <summary>
/// Works out the income Excess Reportable Income (ERI) and fund equalisation entries cause, at calculation time only.
/// Stored entries and stored dividend/interest events are never modified:
/// <list type="bullet">
/// <item>each income with equalisation linked to it is replaced, in the returned lists only, by a copy with the
/// equalisation deducted;</item>
/// <item>each ERI entry adds a new dividend or interest event (<see cref="ExcessReportableIncome.CreateIncomeEvent"/>).</item>
/// </list>
/// </summary>
public static class ReportingFundIncomeResolver
{
    public static ResolvedIncome Resolve(TaxEventLists taxEventLists)
    {
        // A copy made with "with" has no links, so its reduced amount is not checked against the equalisation.
        List<Dividend> dividends = [.. taxEventLists.Dividends.Select(dividend => dividend.EqualisationLinks.Equalisations.Count == 0
            ? dividend
            : dividend with { Proceed = dividend.EqualisationLinks.DeductFrom(dividend.Proceed) })];
        List<InterestIncome> interestIncomes = [.. taxEventLists.InterestIncomes.Select(interest => interest.EqualisationLinks.Equalisations.Count == 0
            ? interest
            : interest with { Amount = interest.EqualisationLinks.DeductFrom(interest.Amount) })];

        foreach (ExcessReportableIncome eri in taxEventLists.CorporateActions.OfType<ExcessReportableIncome>())
        {
            switch (eri.CreateIncomeEvent())
            {
                case Dividend dividend:
                    dividends.Add(dividend);
                    break;
                case InterestIncome interest:
                    interestIncomes.Add(interest);
                    break;
            }
        }
        return new ResolvedIncome(dividends, interestIncomes);
    }
}
