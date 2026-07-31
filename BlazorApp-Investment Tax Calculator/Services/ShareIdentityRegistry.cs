using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Services;

/// <summary>
/// Builds and holds the <see cref="ShareIdentity"/> of every imported share so that tax events can be matched by
/// share identity instead of by exact ticker string. Events sharing an ISIN are recognised as the same share even
/// when the ticker differs (rename, exchange suffix), events sharing a ticker are recognised as the same share even
/// when the ISIN changed (e.g. re-issue), and shares whose ticker and ISIN both changed (e.g. Newco insertion) can
/// be linked manually with <see cref="LinkShares"/>.
/// </summary>
public class ShareIdentityRegistry
{
    private readonly List<ShareIdentity> _identities = [];
    private readonly Dictionary<string, ShareIdentity> _identityByTicker = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ShareIdentity> _identityByIsin = new(StringComparer.Ordinal);
    private readonly List<ShareIdentityLink> _manualLinks = [];

    public IReadOnlyList<ShareIdentity> Identities => _identities;

    /// <summary>
    /// User declared links between share references (ticker or ISIN), kept so they can be exported and re-imported.
    /// </summary>
    public IReadOnlyList<ShareIdentityLink> ManualLinks => _manualLinks;

    public event Action? OnChange;

    /// <summary>
    /// Rebuild the share identities from the given tax events and attach the resolved identity to each event.
    /// Call whenever the set of imported tax events changes. Manual links are kept and re-applied.
    /// </summary>
    public void RegisterEvents(IEnumerable<TaxEvent> taxEvents)
    {
        List<TaxEvent> taxEventList = taxEvents as List<TaxEvent> ?? [.. taxEvents];
        _identities.Clear();
        _identityByTicker.Clear();
        _identityByIsin.Clear();
        foreach (TaxEvent taxEvent in taxEventList)
        {
            RegisterSingleEvent(taxEvent);
        }
        ApplyManualLinks();
        IndexPrimaryTickers();
        foreach (TaxEvent taxEvent in taxEventList)
        {
            taxEvent.ShareIdentity = ResolveByTicker(taxEvent.AssetName);
        }
        OnChange?.Invoke();
    }

    /// <summary>
    /// The identity holding the given ticker (any recorded variation or the primary ticker), or null if unknown.
    /// </summary>
    public ShareIdentity? ResolveByTicker(string ticker)
    {
        if (string.IsNullOrEmpty(ticker)) return null;
        return _identityByTicker.GetValueOrDefault(ticker);
    }

    /// <summary>
    /// The identity holding the given ticker or ISIN, or null if unknown.
    /// </summary>
    public ShareIdentity? Resolve(string tickerOrIsin)
    {
        if (string.IsNullOrEmpty(tickerOrIsin)) return null;
        return _identityByTicker.GetValueOrDefault(tickerOrIsin) ?? _identityByIsin.GetValueOrDefault(tickerOrIsin);
    }

    /// <summary>
    /// The single name all variations of a ticker resolve to, used as grouping key and display name.
    /// An unknown ticker resolves to itself.
    /// </summary>
    public string GetCanonicalTicker(string ticker) => ResolveByTicker(ticker)?.PrimaryTicker ?? ticker;

    /// <summary>
    /// Whether two tickers refer to the same share. Tickers unknown to the registry only match themselves.
    /// </summary>
    public bool IsSameShare(string ticker1, string ticker2)
    {
        ShareIdentity? identity1 = ResolveByTicker(ticker1);
        ShareIdentity? identity2 = ResolveByTicker(ticker2);
        if (identity1 is not null && identity2 is not null) return ReferenceEquals(identity1, identity2);
        if (identity1 is not null) return identity1.MatchesTicker(ticker2);
        if (identity2 is not null) return identity2.MatchesTicker(ticker1);
        return string.Equals(ticker1, ticker2, StringComparison.Ordinal);
    }

    /// <summary>
    /// Record that two share references (each a ticker or an ISIN) refer to the same share, e.g. the old and the
    /// new company of a Newco insertion where both ticker and ISIN changed. The link is persisted with exported
    /// data and re-applied on every registration.
    /// </summary>
    public void LinkShares(string reference, string linkedReference)
    {
        if (string.IsNullOrWhiteSpace(reference) || string.IsNullOrWhiteSpace(linkedReference)) return;
        reference = reference.Trim();
        linkedReference = linkedReference.Trim();
        if (reference == linkedReference) return;
        if (_manualLinks.Exists(link => IsSameLink(link, reference, linkedReference))) return;
        _manualLinks.Add(new ShareIdentityLink(reference, linkedReference));
        ApplyManualLinks();
        IndexPrimaryTickers();
        OnChange?.Invoke();
    }

    /// <summary>
    /// Remove a previously added manual link. Takes effect for matching after the next <see cref="RegisterEvents"/>.
    /// </summary>
    public void RemoveLink(ShareIdentityLink linkToRemove)
    {
        _manualLinks.Remove(linkToRemove);
        OnChange?.Invoke();
    }

    /// <summary>
    /// Load persisted manual links (e.g. from an imported data file) without discarding existing ones.
    /// </summary>
    public void ImportManualLinks(IEnumerable<ShareIdentityLink> links)
    {
        foreach (ShareIdentityLink link in links)
        {
            if (string.IsNullOrWhiteSpace(link.Reference) || string.IsNullOrWhiteSpace(link.LinkedReference)) continue;
            if (_manualLinks.Exists(existing => IsSameLink(existing, link.Reference, link.LinkedReference))) continue;
            _manualLinks.Add(link);
        }
    }

    public void Clear()
    {
        _identities.Clear();
        _identityByTicker.Clear();
        _identityByIsin.Clear();
        _manualLinks.Clear();
        OnChange?.Invoke();
    }

    private static bool IsSameLink(ShareIdentityLink link, string reference, string linkedReference) =>
        (link.Reference == reference && link.LinkedReference == linkedReference) ||
        (link.Reference == linkedReference && link.LinkedReference == reference);

    private void RegisterSingleEvent(TaxEvent taxEvent)
    {
        string ticker = taxEvent.AssetName;
        if (string.IsNullOrEmpty(ticker)) return;
        string isin = taxEvent.Isin;
        ShareIdentity? tickerIdentity = _identityByTicker.GetValueOrDefault(ticker);
        ShareIdentity? isinIdentity = string.IsNullOrEmpty(isin) ? null : _identityByIsin.GetValueOrDefault(isin);
        ShareIdentity identity;
        if (tickerIdentity is not null && isinIdentity is not null && !ReferenceEquals(tickerIdentity, isinIdentity))
        {
            // The ticker is known to one identity and the ISIN to another: both describe the same share, merge them.
            Merge(tickerIdentity, isinIdentity);
            identity = tickerIdentity;
        }
        else
        {
            identity = tickerIdentity ?? isinIdentity ?? CreateIdentity(ticker, taxEvent.Date);
        }
        identity.RecordObservation(ticker, isin, taxEvent.Date);
        _identityByTicker[ticker] = identity;
        if (!string.IsNullOrEmpty(isin))
        {
            _identityByIsin[isin] = identity;
        }
        if (taxEvent is Trade { AssetType: AssetCategoryType.STOCK or AssetCategoryType.FUND } trade)
        {
            identity.AddFullName(GetFullName(trade), taxEvent.Date);
        }
    }

    private ShareIdentity CreateIdentity(string ticker, DateTime lastSeen)
    {
        ShareIdentity identity = new(ticker, lastSeen);
        _identities.Add(identity);
        return identity;
    }

    private void Merge(ShareIdentity keep, ShareIdentity absorbed)
    {
        keep.MergeFrom(absorbed);
        _identities.Remove(absorbed);
        foreach (string ticker in absorbed.Tickers) _identityByTicker[ticker] = keep;
        foreach (string isin in absorbed.Isins) _identityByIsin[isin] = keep;
    }

    private void ApplyManualLinks()
    {
        foreach (ShareIdentityLink link in _manualLinks)
        {
            ShareIdentity? identity = Resolve(link.Reference);
            ShareIdentity? linkedIdentity = Resolve(link.LinkedReference);
            if (identity is null || linkedIdentity is null || ReferenceEquals(identity, linkedIdentity)) continue;
            Merge(identity, linkedIdentity);
        }
    }

    /// <summary>
    /// The primary ticker can be a base symbol that never occurred in the data (e.g. "CWR" for "CWRl" + "CWRm"),
    /// index it so lookups by the displayed name find the identity. A real ticker of another identity wins.
    /// </summary>
    private void IndexPrimaryTickers()
    {
        foreach (ShareIdentity identity in _identities)
        {
            _identityByTicker.TryAdd(identity.PrimaryTicker, identity);
        }
    }

    /// <summary>
    /// The description of a stock/fund trade holds the full company name in broker exports (e.g. IBKR).
    /// Trade descriptions can gain appended trade event lines after calculation, only the first line is the name.
    /// </summary>
    private static string GetFullName(Trade trade)
    {
        string description = trade.Description;
        if (string.IsNullOrWhiteSpace(description)) return string.Empty;
        int lineBreak = description.IndexOfAny(['\r', '\n']);
        return (lineBreak >= 0 ? description[..lineBreak] : description).Trim();
    }
}
