using InvestmentTaxCalculator.Model.Interfaces;
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
/// <item>each ERI entry adds a new dividend or interest event (<see cref="ExcessReportableIncome.CreateIncomeEvent"/>);</item>
/// <item>each income with equalisation linked to it is replaced, in the returned lists only, by a copy with the
/// equalisation deducted.</item>
/// </list>
/// </summary>
public static class ReportingFundIncomeResolver
{
    public static ResolvedIncome Resolve(IDividendLists storedIncome, IEnumerable<CorporateAction> corporateActions)
    {
        List<Dividend> dividends = [.. storedIncome.Dividends];
        List<InterestIncome> interestIncomes = [.. storedIncome.InterestIncomes];
        // Income built from an ERI entry carries the equalisation linked to that entry.
        Dictionary<TaxEvent, IEqualisableIncome> eriOf = new(ReferenceEqualityComparer.Instance);
        foreach (ExcessReportableIncome eri in corporateActions.OfType<ExcessReportableIncome>())
        {
            TaxEvent income = eri.CreateIncomeEvent();
            eriOf[income] = eri;
            if (income is Dividend dividend) dividends.Add(dividend);
            else interestIncomes.Add((InterestIncome)income);
        }

        for (int i = 0; i < dividends.Count; i++)
        {
            Dividend source = dividends[i];
            if (EqualisationOf(source, eriOf) is { } links)
            {
                // A copy made with "with" is not linked, so the reduced amount is not checked against the equalisation.
                dividends[i] = source with { Proceed = Deduct(source.Proceed, links) };
            }
        }
        for (int i = 0; i < interestIncomes.Count; i++)
        {
            InterestIncome source = interestIncomes[i];
            if (EqualisationOf(source, eriOf) is { } links)
            {
                interestIncomes[i] = source with { Amount = Deduct(source.Amount, links) };
            }
        }
        return new ResolvedIncome(dividends, interestIncomes);
    }

    /// <summary>
    /// The equalisation to deduct from an income in the resolved lists: that linked to the stored income itself, or
    /// for income built from an ERI entry, that linked to the ERI entry. Null when there is none.
    /// </summary>
    private static EqualisationLinks? EqualisationOf(TaxEvent income, Dictionary<TaxEvent, IEqualisableIncome> eriOf)
    {
        IEqualisableIncome? owner = eriOf.GetValueOrDefault(income) ?? income as IEqualisableIncome;
        return owner?.EqualisationLinks is { Equalisations.Count: > 0 } links ? links : null;
    }

    /// <summary>
    /// The income amount less its equalisation, in the income's own currency and FX rate.
    /// </summary>
    private static DescribedMoney Deduct(DescribedMoney income, EqualisationLinks links)
    {
        WrappedMoney deductionBase = links.Total;
        WrappedMoney deductionLocal = new(deductionBase.Amount / income.FxRate, income.Amount.Currency);
        return new DescribedMoney
        {
            Amount = income.Amount - deductionLocal,
            FxRate = income.FxRate,
            Description = income.Description + $" (Reduced by equalisation of {deductionBase})"
        };
    }
}
