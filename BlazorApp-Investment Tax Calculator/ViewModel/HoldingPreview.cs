using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;

namespace InvestmentTaxCalculator.ViewModel;

/// <summary>
/// The holding an entry form previews a corporate action against.
/// <para>
/// Recording a new action is straightforward: the pool on the chosen date is the basis. Editing an existing one is
/// not, because the pools already have that action applied to them - previewing a stock split against them applies
/// the ratio twice, and a takeover reads as a holding of zero because it emptied the pool. In that case the basis is
/// the quantity recorded immediately before the action ran.
/// </para>
/// </summary>
public static class HoldingPreview
{
    /// <summary>
    /// Units to preview against, or null when there is no basis to show and the caller should say so rather than
    /// display a number. That happens when an action being edited has been moved to a different date: where it
    /// would then sort among that date's trades is only settled by a calculation, so no answer is available yet.
    /// </summary>
    /// <param name="section104Pools">The pools as they stand after the last calculation.</param>
    /// <param name="assetName">Ticker whose holding is being previewed.</param>
    /// <param name="date">Date currently entered on the form.</param>
    /// <param name="actionBeingEdited">The action the form is editing, or null when recording a new one.</param>
    public static decimal? GetQuantity(UkSection104Pools section104Pools, string? assetName, DateTime date, CorporateAction? actionBeingEdited)
    {
        if (string.IsNullOrWhiteSpace(assetName)) return 0m;

        // GetExistingOrNull rather than GetExistingOrInitialise: tickers are selectable before a calculation has
        // run, so a lookup must not leave an empty pool behind for one that has no pool yet.
        UkSection104? section104 = section104Pools.GetExistingOrNull(assetName);
        if (section104 is null) return 0m;

        if (actionBeingEdited is null)
        {
            return section104.GetLastSection104History(DateOnly.FromDateTime(date))?.NewQuantity ?? 0m;
        }

        if (DateOnly.FromDateTime(date) != DateOnly.FromDateTime(actionBeingEdited.Date))
        {
            return null;
        }

        // Null here means the action moved nothing in this pool, which is a holding of zero as far as the form is
        // concerned - distinct from the moved-date case above, where no answer exists yet.
        return section104.GetQuantityBefore(actionBeingEdited) ?? 0m;
    }
}
