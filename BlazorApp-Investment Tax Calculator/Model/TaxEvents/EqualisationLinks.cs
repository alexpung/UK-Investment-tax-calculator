namespace InvestmentTaxCalculator.Model.TaxEvents;

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
    /// The equalisation entries attached to this income.
    /// </summary>
    EqualisationLinks EqualisationLinks { get; }
}

/// <summary>
/// The income side of the two-way link between an income and its fund equalisation entries (the other side is
/// <see cref="FundEqualisation.LinkedIncome"/>). Guards the rule that the equalisation attached to an income can
/// never exceed it: attaching an entry and changing the income amount are both checked here.
/// <para>
/// Not part of the owning record's value: all instances compare equal, so record equality and hashing are
/// unaffected. A copy of the income made with <c>with</c> gets fresh, empty links (see <see cref="For"/>), so
/// copies such as the reduced income built for the tax calculation are never linked.
/// </para>
/// </summary>
public sealed class EqualisationLinks
{
    private readonly List<FundEqualisation> _equalisations = [];

    private EqualisationLinks(IEqualisableIncome owner)
    {
        Owner = owner;
    }

    public IEqualisableIncome Owner { get; }

    public IReadOnlyList<FundEqualisation> Equalisations => _equalisations;

    public WrappedMoney Total => _equalisations.Sum(e => e.Amount.BaseCurrencyAmount);

    /// <summary>
    /// The links held in <paramref name="field"/> if they belong to <paramref name="owner"/>, otherwise new empty
    /// links for it. A <c>with</c> copy inherits the original's field, which this replaces on first use.
    /// </summary>
    public static EqualisationLinks For(IEqualisableIncome owner, ref EqualisationLinks? field)
    {
        if (field is null || !ReferenceEquals(field.Owner, owner)) field = new EqualisationLinks(owner);
        return field;
    }

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

    internal void Attach(FundEqualisation equalisation)
    {
        WrappedMoney newTotal = Total + equalisation.Amount.BaseCurrencyAmount;
        if (newTotal.Amount > Owner.EqualisableAmount.BaseCurrencyAmount.Amount)
        {
            throw new InvalidOperationException(
                $"Equalisation of {equalisation.Amount.BaseCurrencyAmount} cannot exceed the income not already equalised ({Owner.EqualisableAmount.BaseCurrencyAmount - Total}).");
        }
        _equalisations.Add(equalisation);
    }

    internal void Detach(FundEqualisation equalisation) => _equalisations.RemoveAll(e => ReferenceEquals(e, equalisation));

    public override bool Equals(object? obj) => obj is EqualisationLinks;

    public override int GetHashCode() => 0;
}
