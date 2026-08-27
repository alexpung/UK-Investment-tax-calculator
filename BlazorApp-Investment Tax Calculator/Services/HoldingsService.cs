using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Services;

/// <summary>
/// One movement in the number of units of an asset actually held.
/// </summary>
/// <param name="Date">Date of the movement.</param>
/// <param name="Change">Signed change in units held.</param>
/// <param name="RunningTotal">Units held after the movement.</param>
/// <param name="Description">Short one line description of what caused the movement.</param>
public sealed record HoldingChange(DateTime Date, decimal Change, decimal RunningTotal, string Description);

/// <summary>
/// Units of an asset held on a date, with the movements that produced them.
/// </summary>
public sealed record AssetHolding(decimal Quantity, IReadOnlyList<HoldingChange> Changes)
{
    public static readonly AssetHolding Empty = new(0m, []);
}

/// <summary>
/// Tracks how many units of an asset are actually held on a given date, by running through the imported trades and
/// the corporate actions that change a unit count.
/// <para>
/// This is deliberately NOT the Section 104 pool quantity. The pool is a CGT computational artefact: units matched
/// by the same day or bed and breakfast rules (TCGA92/S105-S106A) never enter it, so between a disposal and the
/// acquisition it is matched with - up to 30 days - the pool reads higher than the units actually held. What is
/// held is the right basis for excess reportable income, whose liability is fixed by the holding at the end of the
/// fund reporting period (SI 2009/3001 reg. 94(3)), and for the holding shown on the corporate action entry forms.
/// </para>
/// <para>
/// Every asset is walked in one chronological pass because corporate actions move units between tickers: the units
/// received in a takeover or a spinoff depend on the holding of the source ticker at that date.
/// </para>
/// </summary>
public class HoldingsService(TaxEventLists taxEventLists, ShareIdentityRegistry shareIdentityRegistry)
{
    private Dictionary<string, List<HoldingChange>> _changesByAsset = [];
    private string _cachedSignature = string.Empty;
    private bool _hasCachedResult;

    /// <summary>
    /// Units of <paramref name="assetName"/> held at the end of <paramref name="asOfDate"/>, with the movements up
    /// to that date. Ticker variations resolve through the share identity registry, so any recorded name of a share
    /// returns the same holding.
    /// </summary>
    public AssetHolding GetHolding(string assetName, DateOnly asOfDate)
    {
        if (string.IsNullOrWhiteSpace(assetName)) return AssetHolding.Empty;

        EnsureBuilt();
        if (!_changesByAsset.TryGetValue(shareIdentityRegistry.GetCanonicalTicker(assetName), out List<HoldingChange>? changes))
        {
            return AssetHolding.Empty;
        }

        List<HoldingChange> changesUpToDate = [.. changes.Where(change => DateOnly.FromDateTime(change.Date) <= asOfDate)];
        return changesUpToDate.Count == 0
            ? AssetHolding.Empty
            : new AssetHolding(changesUpToDate[^1].RunningTotal, changesUpToDate);
    }

    /// <summary>Discard the cached walk, so the next query rebuilds it. Exposed for tests and explicit refreshes.</summary>
    public void Invalidate() => _hasCachedResult = false;

    private void EnsureBuilt()
    {
        string signature = BuildSignature();
        if (_hasCachedResult && string.Equals(_cachedSignature, signature, StringComparison.Ordinal)) return;

        _changesByAsset = BuildChangesByAsset();
        _cachedSignature = signature;
        _hasCachedResult = true;
    }

    /// <summary>
    /// Cache key over everything the walk reads. Share identity state is included because the canonical names that
    /// group the walk change when shares are linked or unlinked without any event being added or removed.
    /// </summary>
    private string BuildSignature() => string.Join('|',
        taxEventLists.Trades.Count,
        taxEventLists.CorporateActions.Count,
        shareIdentityRegistry.Identities.Count,
        shareIdentityRegistry.ManualLinks.Count);

    private Dictionary<string, List<HoldingChange>> BuildChangesByAsset()
    {
        Dictionary<string, List<HoldingChange>> changesByAsset = new(StringComparer.Ordinal);
        Dictionary<string, decimal> runningQuantities = new(StringComparer.Ordinal);

        // Only shares and funds: the entry forms using this are share and fund only, and an option or future
        // "quantity" counts contracts, which must not be pooled together with the underlying.
        IEnumerable<TaxEvent> orderedEvents = taxEventLists.Trades
            .Where(trade => trade.AssetType is AssetCategoryType.STOCK or AssetCategoryType.FUND)
            .Cast<TaxEvent>()
            .Concat(taxEventLists.CorporateActions)
            // A corporate action on a date acts on the holding produced by that date's trades, so it sorts last.
            .OrderBy(taxEvent => taxEvent.Date)
            .ThenBy(taxEvent => taxEvent is CorporateAction ? 1 : 0);

        foreach (TaxEvent taxEvent in orderedEvents)
        {
            switch (taxEvent)
            {
                case Trade trade:
                    RecordChange(changesByAsset, runningQuantities, trade.CanonicalAssetName, trade.Date, trade.RawQuantity, DescribeTrade(trade));
                    break;
                case StockSplit split:
                    ApplyStockSplit(changesByAsset, runningQuantities, split);
                    break;
                case PartnerTransferCorporateAction partnerTransfer:
                    decimal transferChange = partnerTransfer.Direction == PartnerTransferDirection.GiftToPartner
                        ? -partnerTransfer.Quantity
                        : partnerTransfer.Quantity;
                    RecordChange(changesByAsset, runningQuantities, partnerTransfer.CanonicalAssetName, partnerTransfer.Date, transferChange, partnerTransfer.Reason);
                    break;
                case TakeoverCorporateAction takeover:
                    ApplyTakeover(changesByAsset, runningQuantities, takeover);
                    break;
                case SpinoffCorporateAction spinoff:
                    ApplySpinoff(changesByAsset, runningQuantities, spinoff);
                    break;
                // Excess reportable income, fund equalisation and return of capital adjust cost only, never units.
                default:
                    break;
            }
        }

        return changesByAsset;
    }

    private void ApplyStockSplit(Dictionary<string, List<HoldingChange>> changesByAsset, Dictionary<string, decimal> runningQuantities, StockSplit split)
    {
        string assetKey = split.CanonicalAssetName;
        decimal heldQuantity = runningQuantities.GetValueOrDefault(assetKey);
        if (heldQuantity == 0) return;

        decimal splitQuantity = heldQuantity * split.SplitTo / split.SplitFrom;
        // Cash in lieu is paid for the fractional entitlement, so only whole units remain held.
        if (split.CashInLieu is not null) splitQuantity = Math.Floor(splitQuantity);
        RecordChange(changesByAsset, runningQuantities, assetKey, split.Date, splitQuantity - heldQuantity, split.Reason);
    }

    private void ApplyTakeover(Dictionary<string, List<HoldingChange>> changesByAsset, Dictionary<string, decimal> runningQuantities, TakeoverCorporateAction takeover)
    {
        string oldAssetKey = takeover.CanonicalAssetName;
        decimal heldQuantity = runningQuantities.GetValueOrDefault(oldAssetKey);
        if (heldQuantity == 0) return;

        RecordChange(changesByAsset, runningQuantities, oldAssetKey, takeover.Date, -heldQuantity, takeover.Reason);
        string newAssetKey = shareIdentityRegistry.GetCanonicalTicker(takeover.AcquiringCompanyTicker);
        RecordChange(changesByAsset, runningQuantities, newAssetKey, takeover.Date, heldQuantity * takeover.OldToNewRatio, takeover.Reason);
    }

    private void ApplySpinoff(Dictionary<string, List<HoldingChange>> changesByAsset, Dictionary<string, decimal> runningQuantities, SpinoffCorporateAction spinoff)
    {
        // The parent holding is unchanged by a spinoff; only its cost base is split.
        decimal parentQuantity = runningQuantities.GetValueOrDefault(spinoff.CanonicalAssetName);
        if (parentQuantity == 0) return;

        decimal rawSpinoffQuantity = parentQuantity * spinoff.SpinoffSharesPerParentShare;
        // Mirrors SpinoffCorporateAction.ProcessParentCompany: whole units when cash in lieu settles the fraction.
        decimal spinoffQuantity = spinoff.CashInLieu is not null
            ? Math.Floor(rawSpinoffQuantity)
            : Math.Round(rawSpinoffQuantity, 4, MidpointRounding.ToZero);
        if (spinoffQuantity == 0) return;

        string spinoffAssetKey = shareIdentityRegistry.GetCanonicalTicker(spinoff.SpinoffCompanyTicker);
        RecordChange(changesByAsset, runningQuantities, spinoffAssetKey, spinoff.Date, spinoffQuantity, spinoff.Reason);
    }

    private static void RecordChange(Dictionary<string, List<HoldingChange>> changesByAsset, Dictionary<string, decimal> runningQuantities,
                                     string assetKey, DateTime date, decimal change, string description)
    {
        if (change == 0) return;
        decimal runningTotal = runningQuantities.GetValueOrDefault(assetKey) + change;
        runningQuantities[assetKey] = runningTotal;
        if (!changesByAsset.TryGetValue(assetKey, out List<HoldingChange>? changes))
        {
            changes = [];
            changesByAsset[assetKey] = changes;
        }
        changes.Add(new HoldingChange(date, change, runningTotal, description));
    }

    private static string DescribeTrade(Trade trade) =>
        trade.AcquisitionDisposal == TradeType.ACQUISITION
            ? $"Bought {trade.Quantity:0.####} unit(s)"
            : $"Sold {trade.Quantity:0.####} unit(s)";
}
