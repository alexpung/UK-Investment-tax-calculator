using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

namespace InvestmentTaxCalculator.ViewModel;

/// <summary>
/// One quantity changing movement of a Section 104 pool, as shown in the holding breakdown.
/// </summary>
/// <param name="Date">Date of the movement.</param>
/// <param name="Change">Signed quantity change.</param>
/// <param name="RunningTotal">Pool quantity after the movement.</param>
/// <param name="Description">Short one line description of what caused the movement.</param>
public sealed record HoldingChangeRow(DateTime Date, decimal Change, decimal RunningTotal, string Description);

/// <summary>
/// The holding of an asset on a given date together with the recent pool movements that produced it, so a quantity
/// shown on an entry form can be checked against where it came from instead of having to be taken on trust.
/// <para>
/// The movement list is capped at <see cref="MaxRows"/> entries. Earlier movements are collapsed into
/// <see cref="OpeningQuantity"/> rather than dropped, so the rows on screen still reconcile to <see cref="Quantity"/>:
/// a breakdown whose visible rows do not add up to the headline figure would undermine the point of showing it.
/// </para>
/// </summary>
public sealed class HoldingBreakdownViewModel
{
    /// <summary>Maximum number of movement rows listed. Earlier movements become the opening balance.</summary>
    public const int MaxRows = 10;

    public required string AssetName { get; init; }
    public required DateOnly AsOfDate { get; init; }

    /// <summary>Pool quantity on <see cref="AsOfDate"/>, matching what the entry forms display.</summary>
    public required decimal Quantity { get; init; }

    /// <summary>Pool quantity before the first listed movement, covering every movement not listed.</summary>
    public required decimal OpeningQuantity { get; init; }

    /// <summary>How many movements on or before <see cref="AsOfDate"/> are collapsed into the opening balance.</summary>
    public required int OmittedChangeCount { get; init; }

    public required IReadOnlyList<HoldingChangeRow> Rows { get; init; }

    /// <summary>Whether the pool has any recorded movement on or before <see cref="AsOfDate"/>.</summary>
    public bool HasHistory => Rows.Count > 0;

    /// <summary>
    /// Build the breakdown from the Section 104 pools. <paramref name="assetName"/> is resolved through the share
    /// identity registry by the pools, so any recorded ticker variation finds the same pool.
    /// </summary>
    public static HoldingBreakdownViewModel Build(UkSection104Pools section104Pools, string assetName, DateOnly asOfDate)
    {
        UkSection104? section104 = section104Pools.GetExistingOrNull(assetName);
        // Read the headline quantity exactly as the entry forms do, so the breakdown can never disagree with the
        // number it explains - including for value only adjustments, which carry the quantity forward unchanged.
        decimal quantity = section104?.GetLastSection104History(asOfDate)?.NewQuantity ?? 0m;

        List<Section104History> quantityChanges = section104 is null
            ? []
            : [.. section104.Section104HistoryList.Where(history => DateOnly.FromDateTime(history.Date) <= asOfDate && history.QuantityChange != 0)];

        int omittedCount = Math.Max(0, quantityChanges.Count - MaxRows);
        List<Section104History> listedChanges = [.. quantityChanges.Skip(omittedCount)];

        return new HoldingBreakdownViewModel
        {
            AssetName = assetName,
            AsOfDate = asOfDate,
            Quantity = quantity,
            OpeningQuantity = listedChanges.Count > 0 ? listedChanges[0].OldQuantity : 0m,
            OmittedChangeCount = omittedCount,
            Rows = [.. listedChanges.Select(history => new HoldingChangeRow(history.Date, history.QuantityChange, history.NewQuantity, DescribeChange(history)))]
        };
    }

    /// <summary>
    /// A one line description of a movement. The stored explanation of a trade acquisition is a multi line cost
    /// breakdown that would swamp a compact table, so trades get a short label and only pool adjustments made
    /// outside a trade (splits, partner transfers) fall back to their own explanation.
    /// </summary>
    private static string DescribeChange(Section104History history)
    {
        if (history.TradeTaxCalculation is CorporateActionTaxCalculation corporateActionCalculation)
        {
            return corporateActionCalculation.RelatedCorporateAction.Reason;
        }
        if (history.TradeTaxCalculation is { } tradeTaxCalculation)
        {
            return tradeTaxCalculation.AcquisitionDisposal == TradeType.ACQUISITION ? "Acquisition" : "Disposal";
        }
        return string.IsNullOrWhiteSpace(history.Explanation) ? "Pool adjustment" : FirstLine(history.Explanation);
    }

    private static string FirstLine(string text)
    {
        int lineBreakIndex = text.IndexOfAny(['\r', '\n']);
        return lineBreakIndex < 0 ? text.Trim() : text[..lineBreakIndex].Trim();
    }
}
