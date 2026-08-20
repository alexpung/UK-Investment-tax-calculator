using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Services;

/// <summary>
/// Builds and holds the <see cref="ShareIdentity"/> of every imported share so that tax events can be matched by
/// share identity instead of by exact ticker string. Events sharing an ISIN are recognised as the same share even
/// when the ticker differs (rename, exchange suffix). A ticker match alone only joins two shares automatically when
/// there is no conflicting ISIN evidence (e.g. one of the events carries no ISIN, such as a stock split). A ticker
/// carrying a genuinely new ISIN is NOT auto-joined to whatever currently owns that ticker: the same ticker/new-ISIN
/// pattern is produced both by a legitimate re-issue (same company, new ISIN) and by an unrelated company later
/// being assigned a ticker recycled from a delisted one, and the two cannot be told apart from the data alone. Such
/// cases, like a Newco insertion where both ticker and ISIN change, require a manual link via <see cref="LinkShares"/>.
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
        IndexUniqueTickers();
        foreach (TaxEvent taxEvent in taxEventList)
        {
            taxEvent.ShareIdentity = ResolveForEvent(taxEvent);
        }
        OnChange?.Invoke();
    }

    /// <summary>
    /// Resolve the identity for a specific event, preferring its own ISIN (a reliable unique identifier) over the
    /// ticker when both are known and point to different identities, e.g. when the ticker has been reused by an
    /// unrelated share and only a ticker lookup would otherwise pick the wrong one.
    /// </summary>
    private ShareIdentity? ResolveForEvent(TaxEvent taxEvent)
    {
        if (!string.IsNullOrEmpty(taxEvent.Isin) && _identityByIsin.TryGetValue(taxEvent.Isin, out ShareIdentity? byIsin))
        {
            return byIsin;
        }
        return ResolveByTicker(taxEvent.AssetName);
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
    /// Unique across shares, so a ticker recycled by an unrelated company does not resolve to the same name as the
    /// original holder of that ticker. An unknown ticker resolves to itself.
    /// </summary>
    public string GetCanonicalTicker(string ticker) => ResolveByTicker(ticker)?.UniqueTicker ?? ticker;

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
        IndexUniqueTickers();
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
        if (isinIdentity is not null)
        {
            // The ISIN has been seen before: trust it over the ticker, even when the ticker currently belongs to a
            // different identity (e.g. a reused ticker), since ISINs reliably identify a single instrument.
            identity = isinIdentity;
            if (tickerIdentity is not null && !ReferenceEquals(tickerIdentity, isinIdentity) && CanJoinOnTickerAlone(tickerIdentity, isin))
            {
                Merge(isinIdentity, tickerIdentity);
            }
        }
        else if (tickerIdentity is not null && CanJoinOnTickerAlone(tickerIdentity, isin))
        {
            // Ticker matches and there is no conflicting ISIN evidence (no ISIN on this event, or the ticker's
            // identity has none recorded yet): safe to treat as the same share.
            identity = tickerIdentity;
        }
        else
        {
            // Either an unknown ticker, or a ticker whose identity already carries a different, confirmed ISIN:
            // don't assume it's the same share (it may be an unrelated company assigned a recycled ticker).
            // Kept as a separate identity unless the user links them explicitly via LinkShares.
            identity = CreateIdentity(ticker, taxEvent.Date);
        }
        identity.RecordObservation(ticker, isin, taxEvent.Date);
        // Only a best-effort fallback for events without an ISIN to key off: reflects whichever identity most
        // recently claimed this ticker, which can be ambiguous when the ticker is shared by two identities.
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

    /// <summary>
    /// Whether it is safe to treat <paramref name="candidateIdentity"/> as the share for a ticker match alone: true
    /// when the event carries no ISIN, the identity has no ISIN recorded yet, or the ISIN already belongs to it.
    /// False means the identity is already confirmed to a different ISIN, so a ticker match alone is not enough
    /// evidence (it may be a re-issue, or it may be an unrelated share that reused the ticker).
    /// </summary>
    private static bool CanJoinOnTickerAlone(ShareIdentity candidateIdentity, string isin) =>
        string.IsNullOrEmpty(isin) || candidateIdentity.Isins.Count == 0 || candidateIdentity.Isins.Contains(isin);

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
    /// Give every identity a grouping name that is unique across the registry, then index it.
    ///
    /// Two identities can share a primary ticker whenever a ticker was recycled by an unrelated company: each keeps
    /// its own identity here, but both would report as e.g. "RCY". Everything downstream (Section 104 pool keys,
    /// trade grouping, duplicate detection) keys on that string, so leaving it ambiguous silently pools two
    /// unrelated shares and lets one take the other's acquisition cost. Clashing identities are therefore
    /// discriminated by their current ISIN, e.g. "RCY (GB00RCYNEW02)".
    ///
    /// The primary ticker can also be a base symbol that never occurred in the data (e.g. "CWR" for "CWRl" +
    /// "CWRm"), so index it too and lookups by the displayed name find the identity. A real ticker of another
    /// identity wins.
    /// </summary>
    private void IndexUniqueTickers()
    {
        foreach (ShareIdentity identity in _identities)
        {
            identity.SetUniqueSuffix(string.Empty);
        }
        foreach (IGrouping<string, ShareIdentity> clashingIdentities in _identities.GroupBy(identity => identity.PrimaryTicker)
                                                                                  .Where(group => group.Count() > 1))
        {
            int ordinal = 0;
            foreach (ShareIdentity identity in clashingIdentities)
            {
                ordinal++;
                identity.SetUniqueSuffix(GetCurrentIsin(identity) ?? $"#{ordinal}");
            }
        }
        foreach (ShareIdentity identity in _identities)
        {
            _identityByTicker.TryAdd(identity.PrimaryTicker, identity);
            _identityByTicker.TryAdd(identity.UniqueTicker, identity);
        }
    }

    /// <summary>
    /// The most recently seen ISIN of the identity, i.e. the one in force after any Newco insertion, or null when
    /// the identity has no ISIN at all (only events without one, such as stock splits).
    /// </summary>
    private static string? GetCurrentIsin(ShareIdentity identity) =>
        identity.Isins.OrderByDescending(isin => identity.GetIsinLastSeen(isin) ?? DateTime.MinValue).FirstOrDefault();

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
