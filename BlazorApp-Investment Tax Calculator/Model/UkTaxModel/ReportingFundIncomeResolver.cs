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
/// or equalisation entry can be deleted on its own and the next calculation reflects it.
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
    /// Whether the stored dividend is income an equalisation entry can reduce: anything but withholding tax. A stored
    /// dividend of the ERI type is a manual entry (income derived from ERI entries is never stored), so it counts.
    /// </summary>
    public static bool IsDistribution(Dividend dividend) => dividend.DividendType is not DividendType.WITHHOLDING;

    /// <summary>
    /// Whether the interest income is a fund distribution an equalisation entry can reduce. ERI interest is reduced
    /// through its ERI entry instead; interest expense and accrued income profit/loss are never fund distributions.
    /// </summary>
    public static bool IsDistribution(InterestIncome interest) =>
        interest.InterestType is not (InterestType.EXCESSREPORTABLEINCOME or InterestType.INTERESTEXPENSE
            or InterestType.ACCURREDINCOMEPROFIT or InterestType.ACCURREDINCOMELOSS);

    /// <summary>
    /// The name of the income type recorded in <see cref="FundEqualisation.IncomeEventType"/> for an income event an
    /// equalisation can reduce: the dividend or interest type of a distribution, or the income type of an ERI entry.
    /// </summary>
    public static string IncomeEventTypeOf(TaxEvent incomeEvent) => incomeEvent switch
    {
        Dividend d => d.DividendType.ToString(),
        InterestIncome i => i.InterestType.ToString(),
        ExcessReportableIncome eri => eri.IncomeType.ToString(),
        _ => string.Empty
    };

    /// <summary>
    /// The ERI entry an ERI equalisation reduces: the ERI of the same asset on the same date, preferring the one of
    /// the income type recorded on the equalisation when an asset has both a dividend and an interest ERI that day.
    /// </summary>
    public static ExcessReportableIncome? FindExcessReportableIncome(FundEqualisation equalisation, IEnumerable<CorporateAction> corporateActions)
    {
        if (equalisation.IncomeSource != EqualisationIncomeSource.ExcessReportableIncome) return null;
        return corporateActions.OfType<ExcessReportableIncome>()
            .Where(eri => IsSameAsset(eri, equalisation) && eri.Date == equalisation.Date)
            .OrderByDescending(eri => IncomeEventTypeOf(eri) == equalisation.IncomeEventType)
            .FirstOrDefault();
    }

    /// <summary>
    /// The fund distribution an equalisation entry reduces: a distribution of the same asset on the same date,
    /// preferring the income type recorded on the equalisation. The type can change after the equalisation is
    /// entered (a fund dividend converted to interest on the "Income taxable dividend selection" page), and older
    /// entries have none, so any distribution that day is accepted otherwise, the largest first.
    /// </summary>
    public static TaxEvent? FindDistribution(FundEqualisation equalisation, IEnumerable<Dividend> dividends, IEnumerable<InterestIncome> interestIncomes)
    {
        if (equalisation.IncomeSource == EqualisationIncomeSource.ExcessReportableIncome) return null;
        return dividends.Where(IsDistribution).Select(d => (Event: (TaxEvent)d, Money: d.Proceed))
            .Concat(interestIncomes.Where(IsDistribution).Select(i => (Event: (TaxEvent)i, Money: i.Amount)))
            .Where(c => IsSameAsset(c.Event, equalisation) && c.Event.Date == equalisation.Date)
            .OrderByDescending(c => IncomeEventTypeOf(c.Event) == equalisation.IncomeEventType)
            .ThenByDescending(c => c.Money.BaseCurrencyAmount.Amount)
            .Select(c => c.Event)
            .FirstOrDefault();
    }

    /// <summary>
    /// The income event an equalisation entry reduces (an <see cref="ExcessReportableIncome"/>, <see cref="Dividend"/>
    /// or <see cref="InterestIncome"/> from <paramref name="taxEventLists"/>), or null if it has none.
    /// </summary>
    public static TaxEvent? FindIncome(FundEqualisation equalisation, TaxEventLists taxEventLists) =>
        (TaxEvent?)FindExcessReportableIncome(equalisation, taxEventLists.CorporateActions)
        ?? FindDistribution(equalisation, taxEventLists.Dividends, taxEventLists.InterestIncomes);

    /// <summary>
    /// Whether the equalisation entry still has income to reduce: its ERI entry or its fund distribution.
    /// </summary>
    public static bool HasIncome(FundEqualisation equalisation, TaxEventLists taxEventLists) => FindIncome(equalisation, taxEventLists) is not null;

    /// <summary>
    /// The amount of an income event an equalisation can reduce, before any reduction.
    /// </summary>
    public static DescribedMoney IncomeMoneyOf(TaxEvent incomeEvent) => incomeEvent switch
    {
        Dividend d => d.Proceed,
        InterestIncome i => i.Amount,
        ExcessReportableIncome eri => eri.Amount,
        _ => throw new ArgumentException($"{incomeEvent.GetType().Name} is not an income event.", nameof(incomeEvent))
    };

    /// <summary>
    /// Groups every equalisation entry that has income by the stored income event it reduces (ERI entry, dividend or
    /// interest). The single matching rule shared by the calculation, the entry form and the edit validation.
    /// </summary>
    public static Dictionary<TaxEvent, List<FundEqualisation>> GroupEqualisationsByIncome(TaxEventLists taxEventLists)
    {
        Dictionary<TaxEvent, List<FundEqualisation>> groups = new(ReferenceEqualityComparer.Instance);
        foreach (FundEqualisation equalisation in taxEventLists.CorporateActions.OfType<FundEqualisation>())
        {
            TaxEvent? income = FindIncome(equalisation, taxEventLists);
            if (income is null) continue;
            if (!groups.TryGetValue(income, out List<FundEqualisation>? list))
            {
                list = [];
                groups[income] = list;
            }
            list.Add(equalisation);
        }
        return groups;
    }

    /// <summary>
    /// The equalisation entries belonging to income they exceed in total (which would leave negative taxable income).
    /// </summary>
    public static HashSet<FundEqualisation> FindOverEqualised(TaxEventLists taxEventLists) =>
        [.. GroupEqualisationsByIncome(taxEventLists)
            .Where(g => g.Value.Sum(e => e.Amount.BaseCurrencyAmount).Amount > IncomeMoneyOf(g.Key).BaseCurrencyAmount.Amount)
            .SelectMany(g => g.Value)];

    /// <summary>
    /// Whether the income and the equalisation refer to the same share. Checked both ways: an event added or edited
    /// since share identities were last registered has no identity yet, and would otherwise only match the exact
    /// ticker spelling.
    /// </summary>
    private static bool IsSameAsset(TaxEvent income, FundEqualisation equalisation) =>
        income.IsSameAsset(equalisation.AssetName) || equalisation.IsSameAsset(income.AssetName);

    /// <summary>
    /// Points each ERI equalisation entry at its ERI entry, so the cost reduction follows the ERI's reporting period.
    /// Run at the start of every calculation since entries can be added and deleted between calculations.
    /// </summary>
    public static void LinkEqualisations(IEnumerable<CorporateAction> corporateActions)
    {
        List<CorporateAction> actions = [.. corporateActions];
        foreach (FundEqualisation equalisation in actions.OfType<FundEqualisation>())
        {
            equalisation.LinkedExcessReportableIncome = FindExcessReportableIncome(equalisation, actions);
        }
    }

    /// <summary>
    /// Works out the dividend and interest income to tax: the stored income, plus the income of each ERI entry, with
    /// each equalisation entry's amount taken off the income it belongs to. Nothing passed in is modified apart from
    /// the calculation-time ERI link on equalisation entries.
    /// </summary>
    public static ResolvedIncome Resolve(IDividendLists storedIncome, IEnumerable<CorporateAction> corporateActions)
    {
        TaxEventLists lists = new()
        {
            Dividends = storedIncome.Dividends,
            InterestIncomes = storedIncome.InterestIncomes,
            CorporateActions = [.. corporateActions]
        };
        LinkEqualisations(lists.CorporateActions);

        List<Dividend> dividends = [.. lists.Dividends];
        List<InterestIncome> interestIncomes = [.. lists.InterestIncomes];
        Dictionary<ExcessReportableIncome, TaxEvent> eriIncome = new(ReferenceEqualityComparer.Instance);
        // An ERI whose income type was not loaded on import still adjusts the Section 104 pool but adds no income.
        foreach (ExcessReportableIncome eri in lists.CorporateActions.OfType<ExcessReportableIncome>().Where(e => !e.IsIncomeExcluded))
        {
            TaxEvent income = eri.CreateIncomeEvent();
            eriIncome[eri] = income;
            if (income is Dividend dividend) dividends.Add(dividend);
            else interestIncomes.Add((InterestIncome)income);
        }

        Dictionary<TaxEvent, List<FundEqualisation>> groups = GroupEqualisationsByIncome(lists);
        HashSet<FundEqualisation> matched = [.. groups.Values.SelectMany(g => g)];
        List<FundEqualisation> unmatched = [.. lists.CorporateActions.OfType<FundEqualisation>().Where(e => !matched.Contains(e))];

        foreach ((TaxEvent storedTarget, List<FundEqualisation> equalisations) in groups)
        {
            TaxEvent? target = storedTarget is ExcessReportableIncome eri ? eriIncome.GetValueOrDefault(eri) : storedTarget;
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

    /// <summary>
    /// The income amount less the total of the equalisation entries, in the income's own currency and FX rate.
    /// </summary>
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
