using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Services;

/// <summary>
/// Adds, edits and removes manually entered Excess Reportable Income (ERI) and fund equalisation entries.
/// <para>
/// Both entries change other events as well as adding a corporate action: an ERI entry also creates the dividend or
/// interest income it is taxed as, and an equalisation entry reduces the amount of the income event it belongs to.
/// Removing an entry therefore has to undo those side effects too, which this service does. The links are found from
/// the persisted data alone (asset, date, currency, FX rate and the description text written when the entry was
/// added), so entries loaded back from an exported session can be reversed in the same way.
/// </para>
/// </summary>
public class EriEqualisationService(TaxEventLists taxEventLists)
{
    public ExcessReportableIncome AddEri(string assetName, DateTime reportingPeriodEndDate, decimal quantity, decimal incomePerShare,
        string currency, decimal fxRate, ExcessReportableIncomeType incomeType, string isin, CountryCode incomeLocation)
    {
        if (string.IsNullOrEmpty(assetName) || quantity <= 0 || incomePerShare <= 0 || fxRate <= 0)
        {
            throw new InvalidOperationException("Please fill in all fields correctly.");
        }

        DateTime distributionDate = reportingPeriodEndDate.AddMonths(6);
        decimal totalAmountLocal = quantity * incomePerShare;

        ExcessReportableIncome eri = new()
        {
            AssetName = assetName,
            Date = distributionDate,
            ReportingPeriodEndDate = reportingPeriodEndDate,
            IncomeType = incomeType,
            Amount = new DescribedMoney(totalAmountLocal, currency, fxRate, $"ERI for {quantity:N4} shares (Period End: {reportingPeriodEndDate:d})"),
            Isin = isin,
            IncomeLocation = incomeLocation
        };

        taxEventLists.CorporateActions.Add(eri);

        if (incomeType == ExcessReportableIncomeType.DIVIDEND)
        {
            taxEventLists.Dividends.Add(new Dividend
            {
                AssetName = assetName,
                Date = distributionDate,
                DividendType = DividendType.EXCESS_REPORTABLE_INCOME,
                Proceed = new DescribedMoney(totalAmountLocal, currency, fxRate, $"Excess Reportable Income (Dividend) from {assetName}"),
                Isin = isin,
                CompanyLocation = incomeLocation
            });
        }
        else
        {
            taxEventLists.InterestIncomes.Add(new InterestIncome
            {
                AssetName = assetName,
                Date = distributionDate,
                InterestType = InterestType.EXCESSREPORTABLEINCOME,
                Amount = new DescribedMoney(totalAmountLocal, currency, fxRate, $"Excess Reportable Income (Interest) from {assetName}"),
                Isin = isin,
                IncomeLocation = incomeLocation
            });
        }
        return eri;
    }

    /// <summary>
    /// The dividend or interest income event created alongside the ERI entry, or null if it no longer exists.
    /// </summary>
    public TaxEvent? FindIncomeForEri(ExcessReportableIncome eri)
    {
        IEnumerable<(TaxEvent Event, DescribedMoney Money)> candidates = eri.IncomeType == ExcessReportableIncomeType.DIVIDEND
            ? taxEventLists.Dividends
                .Where(d => d.DividendType == DividendType.EXCESS_REPORTABLE_INCOME)
                .Select(d => ((TaxEvent)d, d.Proceed))
            : taxEventLists.InterestIncomes
                .Where(i => i.InterestType == InterestType.EXCESSREPORTABLEINCOME)
                .Select(i => ((TaxEvent)i, i.Amount));

        return candidates
            .Where(c => c.Event.IsSameAsset(eri.AssetName) && c.Event.Date == eri.Date
                && c.Money.Amount.Currency == eri.Amount.Amount.Currency && c.Money.FxRate == eri.Amount.FxRate)
            // An income event reduced by equalisation no longer has the ERI amount, so an exact match is preferred
            // but not required.
            .OrderByDescending(c => c.Money.Amount.Amount == eri.Amount.Amount.Amount)
            .Select(c => c.Event)
            .FirstOrDefault();
    }

    /// <summary>
    /// The equalisation entries that reduce the income event created by the ERI entry.
    /// </summary>
    public List<FundEqualisation> GetEqualisationsLinkedToEri(ExcessReportableIncome eri)
    {
        TaxEvent? income = FindIncomeForEri(eri);
        if (income is null) return [];
        return [.. taxEventLists.CorporateActions.OfType<FundEqualisation>().Where(fe => ReferenceEquals(FindIncomeForEqualisation(fe), income))];
    }

    /// <summary>
    /// Removes the ERI entry and the income event created with it. Refused while equalisation entries still reduce
    /// that income, since they would otherwise be left pointing at nothing.
    /// </summary>
    public void RemoveEri(ExcessReportableIncome eri)
    {
        List<FundEqualisation> linkedEqualisations = GetEqualisationsLinkedToEri(eri);
        if (linkedEqualisations.Count > 0)
        {
            throw new InvalidOperationException($"This ERI entry is reduced by {linkedEqualisations.Count} fund equalisation entr{(linkedEqualisations.Count == 1 ? "y" : "ies")}. " +
                "Delete the equalisation entries first.");
        }

        TaxEvent? income = FindIncomeForEri(eri);
        taxEventLists.CorporateActions.Remove(eri);
        switch (income)
        {
            case Dividend dividend:
                taxEventLists.Dividends.Remove(dividend);
                break;
            case InterestIncome interest:
                taxEventLists.InterestIncomes.Remove(interest);
                break;
        }
    }

    /// <summary>
    /// Replaces an ERI entry (and its income event) with one made from the new values.
    /// </summary>
    public ExcessReportableIncome UpdateEri(ExcessReportableIncome existing, string assetName, DateTime reportingPeriodEndDate, decimal quantity,
        decimal incomePerShare, string currency, decimal fxRate, ExcessReportableIncomeType incomeType, string isin, CountryCode incomeLocation)
    {
        if (string.IsNullOrEmpty(assetName) || quantity <= 0 || incomePerShare <= 0 || fxRate <= 0)
        {
            throw new InvalidOperationException("Please fill in all fields correctly.");
        }
        RemoveEri(existing);
        return AddEri(assetName, reportingPeriodEndDate, quantity, incomePerShare, currency, fxRate, incomeType, isin, incomeLocation);
    }

    /// <summary>
    /// Records a fund equalisation entry against a dividend or interest income event and reduces that event's amount.
    /// The equalisation amount is taken in the income event's currency and FX rate.
    /// </summary>
    public FundEqualisation AddEqualisation(string assetName, TaxEvent incomeEvent, decimal equalisationAmount)
    {
        DescribedMoney incomeMoney = GetIncomeMoney(incomeEvent);
        if (equalisationAmount <= 0 || incomeMoney.FxRate <= 0)
        {
            throw new InvalidOperationException("Please select an event and enter a valid amount and exchange rate.");
        }

        decimal availableBaseAmount = incomeMoney.BaseCurrencyAmount.Amount;
        decimal inputBaseAmount = equalisationAmount * incomeMoney.FxRate;
        if (inputBaseAmount > availableBaseAmount)
        {
            throw new InvalidOperationException($"Equalisation amount ({inputBaseAmount:N2} GBP) cannot exceed the income amount ({availableBaseAmount:N2} GBP).");
        }

        DescribedMoney desMoney = new(equalisationAmount, incomeMoney.Amount.Currency, incomeMoney.FxRate, $"{GetIncomeTypeDescription(incomeEvent)} on {incomeEvent.Date:d}");

        // For ERI-related equalisation, record the reporting period end so units disposed of between the period end
        // and the distribution date take their share of the cost reduction at the disposal instead of the pool.
        DateTime? reportingPeriodEnd = null;
        if (IsEriIncome(incomeEvent))
        {
            ExcessReportableIncome? relatedEri = taxEventLists.CorporateActions.OfType<ExcessReportableIncome>()
                .FirstOrDefault(e => e.IsSameAsset(assetName) && e.Date == incomeEvent.Date);
            reportingPeriodEnd = relatedEri is not null
                ? relatedEri.EffectiveReportingPeriodEndDate.ToDateTime(TimeOnly.MinValue)
                : incomeEvent.Date.AddMonths(-6);
        }

        FundEqualisation equalisation = new()
        {
            AssetName = assetName,
            Date = incomeEvent.Date,
            Amount = desMoney,
            RelatedEventDescription = desMoney.Description,
            ReportingPeriodEndDate = reportingPeriodEnd
        };
        taxEventLists.CorporateActions.Add(equalisation);

        WrappedMoney reductionBase = desMoney.BaseCurrencyAmount;
        WrappedMoney reductionLocal = new(reductionBase.Amount / incomeMoney.FxRate, incomeMoney.Amount.Currency);
        ReplaceIncomeMoney(incomeEvent, new DescribedMoney
        {
            Amount = incomeMoney.Amount - reductionLocal,
            FxRate = incomeMoney.FxRate,
            Description = incomeMoney.Description + ReductionNote(reductionBase)
        });
        return equalisation;
    }

    /// <summary>
    /// The dividend or interest income event the equalisation entry reduced, or null if it can no longer be found.
    /// </summary>
    public TaxEvent? FindIncomeForEqualisation(FundEqualisation equalisation)
    {
        string note = ReductionNote(equalisation.Amount.BaseCurrencyAmount);
        IEnumerable<TaxEvent> incomeEvents = taxEventLists.Dividends.Where(d => d.DividendType != DividendType.WITHHOLDING).Cast<TaxEvent>()
            .Concat(taxEventLists.InterestIncomes);

        return incomeEvents
            .Select(e => (Event: e, Money: GetIncomeMoney(e)))
            .Where(c => c.Event.IsSameAsset(equalisation.AssetName) && c.Event.Date == equalisation.Date
                && c.Money.Amount.Currency == equalisation.Amount.Amount.Currency && c.Money.FxRate == equalisation.Amount.FxRate)
            .OrderByDescending(c => c.Money.Description.Contains(note, StringComparison.Ordinal))
            .ThenByDescending(c => equalisation.RelatedEventDescription?.StartsWith(GetIncomeTypeDescription(c.Event) + " on ", StringComparison.Ordinal) ?? false)
            .Select(c => c.Event)
            .FirstOrDefault();
    }

    /// <summary>
    /// Removes the equalisation entry and adds its amount back to the income event it reduced.
    /// Returns the restored income event (interest income records are replaced rather than changed in place).
    /// </summary>
    public TaxEvent? RemoveEqualisation(FundEqualisation equalisation)
    {
        TaxEvent? income = FindIncomeForEqualisation(equalisation);
        taxEventLists.CorporateActions.Remove(equalisation);
        if (income is null) return null;

        DescribedMoney incomeMoney = GetIncomeMoney(income);
        WrappedMoney reductionBase = equalisation.Amount.BaseCurrencyAmount;
        WrappedMoney reductionLocal = new(reductionBase.Amount / incomeMoney.FxRate, incomeMoney.Amount.Currency);
        string note = ReductionNote(reductionBase);
        int noteIndex = incomeMoney.Description.LastIndexOf(note, StringComparison.Ordinal);
        string description = noteIndex >= 0 ? incomeMoney.Description.Remove(noteIndex, note.Length) : incomeMoney.Description;

        return ReplaceIncomeMoney(income, new DescribedMoney
        {
            Amount = incomeMoney.Amount + reductionLocal,
            FxRate = incomeMoney.FxRate,
            Description = description
        });
    }

    /// <summary>
    /// Replaces an equalisation entry with a new amount and/or income event. If the new entry is invalid the
    /// original entry is put back and the error rethrown.
    /// </summary>
    public FundEqualisation UpdateEqualisation(FundEqualisation existing, string assetName, TaxEvent incomeEvent, decimal equalisationAmount)
    {
        TaxEvent? oldIncome = FindIncomeForEqualisation(existing);
        TaxEvent? restoredIncome = RemoveEqualisation(existing);
        // The target may be the event just restored, which for interest income is now a different record.
        TaxEvent target = oldIncome is not null && ReferenceEquals(incomeEvent, oldIncome) && restoredIncome is not null ? restoredIncome : incomeEvent;
        try
        {
            return AddEqualisation(assetName, target, equalisationAmount);
        }
        catch (InvalidOperationException)
        {
            if (restoredIncome is not null)
            {
                AddEqualisation(existing.AssetName, restoredIncome, existing.Amount.Amount.Amount);
            }
            else
            {
                taxEventLists.CorporateActions.Add(existing);
            }
            throw;
        }
    }

    public static bool IsEriIncome(TaxEvent incomeEvent) => incomeEvent is Dividend { DividendType: DividendType.EXCESS_REPORTABLE_INCOME }
        or InterestIncome { InterestType: InterestType.EXCESSREPORTABLEINCOME };

    public static DescribedMoney GetIncomeMoney(TaxEvent incomeEvent) => incomeEvent switch
    {
        Dividend d => d.Proceed,
        InterestIncome i => i.Amount,
        _ => throw new ArgumentException($"{incomeEvent.GetType().Name} is not a dividend or interest income event.", nameof(incomeEvent))
    };

    private static string GetIncomeTypeDescription(TaxEvent incomeEvent) => incomeEvent switch
    {
        Dividend d => d.DividendType.GetDescription(),
        InterestIncome i => i.InterestType.GetDescription(),
        _ => "Income event"
    };

    private static string ReductionNote(WrappedMoney reductionBase) => $" (Reduced by equalisation of {reductionBase})";

    private TaxEvent ReplaceIncomeMoney(TaxEvent incomeEvent, DescribedMoney newMoney)
    {
        switch (incomeEvent)
        {
            case Dividend d:
                d.Proceed = newMoney;
                return d;
            case InterestIncome i:
                // Amount is init-only, so the record is replaced in the list.
                InterestIncome replacement = i with { Amount = newMoney };
                int index = taxEventLists.InterestIncomes.IndexOf(i);
                if (index >= 0) taxEventLists.InterestIncomes[index] = replacement;
                return replacement;
            default:
                throw new ArgumentException($"{incomeEvent.GetType().Name} is not a dividend or interest income event.", nameof(incomeEvent));
        }
    }
}
