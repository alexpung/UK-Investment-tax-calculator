using InvestmentTaxCalculator.Services;

namespace InvestmentTaxCalculator.ViewModel;

/// <summary>
/// The units of an asset held on a date together with the recent movements that produced them, so a quantity shown
/// on an entry form can be checked against where it came from instead of having to be taken on trust.
/// <para>
/// The movement list is capped at <see cref="MaxRows"/> entries. Earlier movements are collapsed into
/// <see cref="OpeningQuantity"/> rather than dropped, so the rows on screen still reconcile to <see cref="Quantity"/>:
/// a breakdown that does not add up would be worse than no breakdown at all.
/// </para>
/// </summary>
public sealed class HoldingBreakdownViewModel
{
    /// <summary>Maximum number of movement rows listed. Earlier movements become the opening balance.</summary>
    public const int MaxRows = 10;

    public required string AssetName { get; init; }
    public required DateOnly AsOfDate { get; init; }

    /// <summary>Units held on <see cref="AsOfDate"/>, matching what the entry forms display.</summary>
    public required decimal Quantity { get; init; }

    /// <summary>Units held before the first listed movement, covering every movement not listed.</summary>
    public required decimal OpeningQuantity { get; init; }

    /// <summary>How many movements on or before <see cref="AsOfDate"/> are collapsed into the opening balance.</summary>
    public required int OmittedChangeCount { get; init; }

    public required IReadOnlyList<HoldingChange> Rows { get; init; }

    /// <summary>Whether any movement is recorded on or before <see cref="AsOfDate"/>.</summary>
    public bool HasHistory => Rows.Count > 0;

    /// <summary>
    /// Whether the holding is ever negative up to <see cref="AsOfDate"/>. Selling units that were never acquired
    /// almost always means an earlier statement is missing rather than a genuine short position, which is close to
    /// unheard of for a fund, so the figure above is not to be trusted.
    /// </summary>
    public bool HasNegativeHolding => Rows.Any(row => row.RunningTotal < 0);

    public static HoldingBreakdownViewModel Build(HoldingsService holdingsService, string assetName, DateOnly asOfDate)
    {
        AssetHolding holding = holdingsService.GetHolding(assetName, asOfDate);

        int omittedCount = Math.Max(0, holding.Changes.Count - MaxRows);
        List<HoldingChange> listedChanges = [.. holding.Changes.Skip(omittedCount)];

        return new HoldingBreakdownViewModel
        {
            AssetName = assetName,
            AsOfDate = asOfDate,
            Quantity = holding.Quantity,
            OpeningQuantity = listedChanges.Count > 0 ? listedChanges[0].RunningTotal - listedChanges[0].Change : 0m,
            OmittedChangeCount = omittedCount,
            Rows = listedChanges
        };
    }
}
