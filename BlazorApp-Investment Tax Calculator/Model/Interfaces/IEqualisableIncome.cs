using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Model.Interfaces;

/// <summary>
/// Income a fund equalisation entry can be attached to: a stored <see cref="Dividend"/>, a stored
/// <see cref="InterestIncome"/> or an <see cref="ExcessReportableIncome"/> entry.
/// </summary>
public interface IEqualisableIncome
{
    /// <summary>
    /// Saved identifier, so the link from <see cref="FundEqualisation.IncomeId"/> survives export and import.
    /// </summary>
    string IncomeId { get; set; }

    /// <summary>
    /// The income amount its equalisation entries may not exceed in total.
    /// </summary>
    DescribedMoney EqualisableAmount { get; }

    /// <summary>
    /// Whether this type of income can have equalisation at all (not withholding tax, interest expense or accrued
    /// income).
    /// </summary>
    bool CanBeEqualised { get; }

    /// <summary>
    /// The equalisation entries attached to this income.
    /// </summary>
    EqualisationLinks EqualisationLinks { get; }
}
