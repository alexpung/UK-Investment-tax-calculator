using InvestmentTaxCalculator.Model.Interfaces;

using System.Runtime.CompilerServices;

namespace InvestmentTaxCalculator.Model.TaxEvents;

/// <summary>
/// The income side of the two-way link between an income and its fund equalisation entries (the other side is
/// <see cref="FundEqualisation.LinkedIncome"/>). Guards the rules that the equalisation attached to an income can
/// never exceed it, and that only income of a type that can be equalised has any.
/// <para>
/// Held per income object, outside the income records, so links are not part of a record's value: equality, hashing
/// and <c>with</c> copies are unaffected, and a copy (such as the reduced income built for the tax calculation)
/// starts with no links.
/// </para>
/// </summary>
public sealed class EqualisationLinks
{
    private static readonly ConditionalWeakTable<IEqualisableIncome, EqualisationLinks> _linksByIncome = [];
    private readonly IEqualisableIncome _income;
    private readonly List<FundEqualisation> _equalisations = [];

    private EqualisationLinks(IEqualisableIncome income)
    {
        _income = income;
    }

    /// <summary>
    /// The links of the given income object.
    /// </summary>
    public static EqualisationLinks Of(IEqualisableIncome income) => _linksByIncome.GetValue(income, i => new EqualisationLinks(i));

    public IReadOnlyList<FundEqualisation> Equalisations => _equalisations;

    public WrappedMoney Total => _equalisations.Sum(e => e.Amount.BaseCurrencyAmount);

    /// <summary>
    /// Throws if the attached equalisation would exceed <paramref name="newAmount"/>. Called by the income before
    /// its amount changes.
    /// </summary>
    public void EnsureCovered(DescribedMoney newAmount)
    {
        if (_equalisations.Count > 0 && Total.Amount > newAmount.BaseCurrencyAmount.Amount)
        {
            throw new InvalidOperationException(
                $"The income ({newAmount.BaseCurrencyAmount}) cannot be less than the fund equalisation entered against it ({Total}). Delete the equalisation first.");
        }
    }

    /// <summary>
    /// Throws if any equalisation is attached. Called by the income before it changes to a type that cannot be equalised.
    /// </summary>
    public void EnsureNone()
    {
        if (_equalisations.Count > 0)
        {
            throw new InvalidOperationException("Income with fund equalisation entered against it cannot be changed to a type that cannot be equalised. Delete the equalisation first.");
        }
    }

    /// <summary>
    /// <paramref name="income"/> less the attached equalisation, in the income's own currency and FX rate. Returns
    /// <paramref name="income"/> itself when there is none.
    /// </summary>
    public DescribedMoney DeductFrom(DescribedMoney income)
    {
        if (_equalisations.Count == 0) return income;
        WrappedMoney deductionLocal = new(Total.Amount / income.FxRate, income.Amount.Currency);
        string note = $"Reduced by equalisation of {Total}";
        return income with
        {
            Amount = income.Amount - deductionLocal,
            // Income entered by hand often has no description: the note then stands alone, not as a bracketed suffix.
            Description = string.IsNullOrWhiteSpace(income.Description) ? note : $"{income.Description} ({note})"
        };
    }

    internal void Attach(FundEqualisation equalisation)
    {
        if (!_income.CanBeEqualised)
        {
            throw new InvalidOperationException("Fund equalisation cannot be entered against this type of income.");
        }
        WrappedMoney available = _income.EqualisableAmount.BaseCurrencyAmount - Total;
        if (equalisation.Amount.BaseCurrencyAmount.Amount > available.Amount)
        {
            throw new InvalidOperationException(
                $"Equalisation of {equalisation.Amount.BaseCurrencyAmount} cannot exceed the income not already equalised ({available}).");
        }
        _equalisations.Add(equalisation);
    }

    internal void Detach(FundEqualisation equalisation) => _equalisations.RemoveAll(e => ReferenceEquals(e, equalisation));
}
