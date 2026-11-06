using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Model.UkTaxModel;

/// <summary>
/// The dividend and interest income to tax once reporting fund entries are taken into account.
/// </summary>
/// <param name="Dividends">Stored dividends plus ERI dividends, with equalisation reductions applied.</param>
/// <param name="InterestIncomes">Stored interest plus ERI interest, with equalisation reductions applied.</param>
/// <param name="UnmatchedEqualisations">Equalisation entries whose income could not be found, so reduce no income.</param>
public record ResolvedIncome(List<Dividend> Dividends, List<InterestIncome> InterestIncomes, List<FundEqualisation> UnmatchedEqualisations);

/// <summary>
/// Turns the stored Excess Reportable Income (ERI) and fund equalisation entries into the income they cause, at
/// calculation time only. The stored entries and the stored dividend/interest events are never modified, so an ERI
/// or equalisation entry can be edited or deleted on its own and the next calculation reflects it.
/// <list type="bullet">
/// <item>Each ERI entry adds a dividend or interest income event (<see cref="ExcessReportableIncome.CreateIncomeEvent"/>).</item>
/// <item>Each equalisation entry reduces the income it was paid with: the ERI entry of the same asset on the same
/// date, or the fund distribution (dividend or interest) of the same asset on the same date. The reduced income is
/// a copy; the stored event keeps its original amount.</item>
/// </list>
/// </summary>
public static class ReportingFundIncomeResolver
{
    /// <summary>
    /// Whether the dividend is a fund distribution an equalisation entry can reduce.
    /// </summary>
    public static bool IsDistribution(Dividend dividend) =>
        dividend.DividendType is not (DividendType.WITHHOLDING or DividendType.EXCESS_REPORTABLE_INCOME);

    /// <summary>
    /// Whether the interest income is a fund distribution an equalisation entry can reduce.
    /// </summary>
    public static bool IsDistribution(InterestIncome interest) =>
        interest.InterestType is not InterestType.EXCESSREPORTABLEINCOME && !interest.IsInterestExpense;

    public static ExcessReportableIncome? FindExcessReportableIncome(FundEqualisation equalisation, IEnumerable<CorporateAction> corporateActions)
    {
        if (equalisation.IncomeSource != EqualisationIncomeSource.ExcessReportableIncome) return null;
        return corporateActions.OfType<ExcessReportableIncome>()
            .FirstOrDefault(eri => eri.IsSameAsset(equalisation.AssetName) && eri.Date == equalisation.Date);
    }

    /// <summary>
    /// The fund distribution an equalisation entry reduces. If more than one distribution of the asset is paid on the
    /// date, the largest is taken.
    /// </summary>
    public static TaxEvent? FindDistribution(FundEqualisation equalisation, IEnumerable<Dividend> dividends, IEnumerable<InterestIncome> interestIncomes)
    {
        if (equalisation.IncomeSource == EqualisationIncomeSource.ExcessReportableIncome) return null;
        return dividends.Where(IsDistribution).Select(d => (Event: (TaxEvent)d, Money: d.Proceed))
            .Concat(interestIncomes.Where(IsDistribution).Select(i => (Event: (TaxEvent)i, Money: i.Amount)))
            .Where(c => c.Event.IsSameAsset(equalisation.AssetName) && c.Event.Date == equalisation.Date)
            .OrderByDescending(c => c.Money.BaseCurrencyAmount.Amount)
            .Select(c => c.Event)
            .FirstOrDefault();
    }

    /// <summary>
    /// Points each ERI equalisation entry at its ERI entry, so the cost reduction follows the ERI's reporting period.
    /// Run at the start of every calculation since entries can be edited between calculations.
    /// </summary>
    public static void LinkEqualisations(IEnumerable<CorporateAction> corporateActions)
    {
        List<CorporateAction> actions = [.. corporateActions];
        foreach (FundEqualisation equalisation in actions.OfType<FundEqualisation>())
        {
            equalisation.LinkedExcessReportableIncome = FindExcessReportableIncome(equalisation, actions);
        }
    }

    public static ResolvedIncome Resolve(IDividendLists storedIncome, IEnumerable<CorporateAction> corporateActions)
    {
        List<CorporateAction> actions = [.. corporateActions];
        LinkEqualisations(actions);

        List<Dividend> dividends = [.. storedIncome.Dividends];
        List<InterestIncome> interestIncomes = [.. storedIncome.InterestIncomes];
        Dictionary<ExcessReportableIncome, TaxEvent> eriIncome = new(ReferenceEqualityComparer.Instance);
        foreach (ExcessReportableIncome eri in actions.OfType<ExcessReportableIncome>())
        {
            TaxEvent income = eri.CreateIncomeEvent();
            eriIncome[eri] = income;
            if (income is Dividend dividend) dividends.Add(dividend);
            else interestIncomes.Add((InterestIncome)income);
        }

        List<FundEqualisation> unmatched = [];
        Dictionary<TaxEvent, List<FundEqualisation>> reductions = new(ReferenceEqualityComparer.Instance);
        foreach (FundEqualisation equalisation in actions.OfType<FundEqualisation>())
        {
            TaxEvent? target = equalisation.LinkedExcessReportableIncome is { } eri
                ? eriIncome[eri]
                : FindDistribution(equalisation, storedIncome.Dividends, storedIncome.InterestIncomes);
            if (target is null)
            {
                unmatched.Add(equalisation);
                continue;
            }
            if (!reductions.TryGetValue(target, out List<FundEqualisation>? list))
            {
                list = [];
                reductions[target] = list;
            }
            list.Add(equalisation);
        }

        foreach ((TaxEvent target, List<FundEqualisation> equalisations) in reductions)
        {
            switch (target)
            {
                case Dividend dividend:
                    int dividendIndex = dividends.FindIndex(d => ReferenceEquals(d, dividend));
                    dividends[dividendIndex] = dividend with { Proceed = Reduce(dividend.Proceed, equalisations) };
                    break;
                case InterestIncome interest:
                    int interestIndex = interestIncomes.FindIndex(i => ReferenceEquals(i, interest));
                    interestIncomes[interestIndex] = interest with { Amount = Reduce(interest.Amount, equalisations) };
                    break;
            }
        }

        return new ResolvedIncome(dividends, interestIncomes, unmatched);
    }

    private static DescribedMoney Reduce(DescribedMoney income, List<FundEqualisation> equalisations)
    {
        WrappedMoney reductionBase = equalisations.Sum(e => e.Amount.BaseCurrencyAmount);
        WrappedMoney reductionLocal = new(reductionBase.Amount / income.FxRate, income.Amount.Currency);
        return new DescribedMoney
        {
            Amount = income.Amount - reductionLocal,
            FxRate = income.FxRate,
            Description = income.Description + $" (Reduced by equalisation of {reductionBase})"
        };
    }
}
