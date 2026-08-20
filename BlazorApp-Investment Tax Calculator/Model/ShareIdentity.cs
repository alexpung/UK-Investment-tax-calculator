namespace InvestmentTaxCalculator.Model;

/// <summary>
/// Records the identity of a share independently of any single ticker string.
/// A share keeps its identity through ticker renames, Newco insertions (which change the ISIN and possibly the
/// ticker) and exchange suffix variations (IBKR appends a lowercase exchange suffix, e.g. "CWRl" vs "CWR"),
/// so all recorded ISINs and ticker variations refer to the same instrument for tax matching purposes.
/// The date each ticker/ISIN combination and company name was last seen in the imported data is recorded, so it
/// is known which ticker, ISIN or name is the older and which is the current one.
/// </summary>
public class ShareIdentity
{
    private readonly List<string> _tickers = [];
    private readonly List<string> _isins = [];
    private readonly List<string> _fullNames = [];
    private readonly Dictionary<string, DateTime> _tickerLastSeen = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTime> _isinLastSeen = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTime> _fullNameLastSeen = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(string Ticker, string Isin), DateTime> _tickerIsinLastSeen = [];
    private string _uniqueSuffix = string.Empty;

    /// <summary>
    /// All ticker variations known for this share, e.g. exchange suffixed symbols and renamed symbols.
    /// </summary>
    public IReadOnlyList<string> Tickers => _tickers;

    /// <summary>
    /// All ISINs known for this share. More than one entry when a Newco insertion or restructuring issued a new ISIN.
    /// </summary>
    public IReadOnlyList<string> Isins => _isins;

    /// <summary>
    /// Full company/fund name(s) when available from the imported data.
    /// </summary>
    public IReadOnlyList<string> FullNames => _fullNames;

    /// <summary>
    /// Every ticker/ISIN combination observed in the imported data with the date it was last seen, ordered from
    /// oldest to most recently seen. Combinations without an ISIN are recorded with an empty ISIN.
    /// </summary>
    public IReadOnlyList<ShareIdentityObservation> Observations => [.. _tickerIsinLastSeen
        .Select(observation => new ShareIdentityObservation(observation.Key.Ticker, observation.Key.Isin, observation.Value))
        .OrderBy(observation => observation.LastSeen)];

    public ShareIdentity(string ticker, DateTime? lastSeen = null)
    {
        AddTicker(ticker, lastSeen);
    }

    /// <summary>
    /// The single display/grouping name of this share:
    /// the common base symbol when all tickers only differ by a lowercase exchange suffix,
    /// otherwise the most recently seen ticker (e.g. the new symbol after a rename).
    /// </summary>
    public string PrimaryTicker
    {
        get
        {
            if (_tickers.Count == 0) return string.Empty;
            if (_tickers.Count == 1) return _tickers[0];
            List<string> baseSymbols = _tickers.Select(StripExchangeSuffix).Distinct().ToList();
            if (baseSymbols.Count == 1 && !string.IsNullOrEmpty(baseSymbols[0])) return baseSymbols[0];
            return _tickers.OrderByDescending(ticker => _tickerLastSeen.GetValueOrDefault(ticker, DateTime.MinValue))
                           .First();
        }
    }

    /// <summary>
    /// The date the given ticker was last seen on a tax event, or null if the ticker is unknown or no dated
    /// observation was recorded. An older date than another ticker's means this is the older ticker.
    /// </summary>
    public DateTime? GetTickerLastSeen(string ticker) =>
        _tickerLastSeen.TryGetValue(ticker, out DateTime lastSeen) ? lastSeen : null;

    /// <summary>
    /// The date the given ISIN was last seen on a tax event, or null if the ISIN is unknown or no dated
    /// observation was recorded. An older date than another ISIN's means this is the older (pre Newco) ISIN.
    /// </summary>
    public DateTime? GetIsinLastSeen(string isin) =>
        _isinLastSeen.TryGetValue(isin, out DateTime lastSeen) ? lastSeen : null;

    /// <summary>
    /// The date the given company name was last seen on a tax event, or null if the name is unknown or no dated
    /// observation was recorded.
    /// </summary>
    public DateTime? GetFullNameLastSeen(string fullName) =>
        _fullNameLastSeen.TryGetValue(fullName, out DateTime lastSeen) ? lastSeen : null;

    /// <summary>
    /// The name this share is grouped and reported under, unique across every identity in the registry.
    /// Equal to <see cref="PrimaryTicker"/> unless another identity resolves to the same primary ticker - a ticker
    /// recycled by an unrelated company - in which case the registry appends the current ISIN to tell the two
    /// apart, e.g. "RCY (GB00RCYNEW02)". Grouping keys must be built from this and not from
    /// <see cref="PrimaryTicker"/>: two unrelated shares sharing a ticker would otherwise collapse into a single
    /// Section 104 pool and take each other's acquisition cost.
    /// </summary>
    public string UniqueTicker => string.IsNullOrEmpty(_uniqueSuffix) ? PrimaryTicker : $"{PrimaryTicker} ({_uniqueSuffix})";

    /// <summary>
    /// Set by the ShareIdentityRegistry once all identities are known: empty when <see cref="PrimaryTicker"/> is
    /// already unique, otherwise the discriminator that makes <see cref="UniqueTicker"/> unique.
    /// </summary>
    public void SetUniqueSuffix(string suffix) => _uniqueSuffix = suffix ?? string.Empty;

    /// <summary>
    /// Whether the given ticker refers to this share: either a ticker actually observed in the imported data, or
    /// the name the share is reported under. <see cref="PrimaryTicker"/> is deliberately not matched on its own:
    /// it can be a synthetic base symbol that was never traded for this share (e.g. "ABC" for "ABCl" + "ABCd")
    /// and that is the real, observed ticker of an unrelated company, which this share must not claim.
    /// When the primary ticker is unambiguous it is matched anyway, because <see cref="UniqueTicker"/> equals it.
    /// </summary>
    public bool MatchesTicker(string ticker) =>
        !string.IsNullOrEmpty(ticker) && (_tickers.Contains(ticker) || ticker == UniqueTicker);

    public bool MatchesIsin(string isin) => !string.IsNullOrEmpty(isin) && _isins.Contains(isin);

    public bool IsSameShare(ShareIdentity other) =>
        ReferenceEquals(this, other) || other._tickers.Exists(_tickers.Contains) || other._isins.Exists(_isins.Contains);

    /// <summary>
    /// Record a ticker/ISIN combination seen on a tax event dated <paramref name="date"/>, keeping the latest
    /// date known for the ticker, the ISIN and the combination of both.
    /// </summary>
    public void RecordObservation(string ticker, string isin, DateTime date)
    {
        if (string.IsNullOrEmpty(ticker)) return;
        AddTicker(ticker, date);
        isin ??= string.Empty;
        if (!string.IsNullOrEmpty(isin)) AddIsin(isin, date);
        (string ticker, string isin) comboKey = (ticker, isin);
        if (date > _tickerIsinLastSeen.GetValueOrDefault(comboKey, DateTime.MinValue))
        {
            _tickerIsinLastSeen[comboKey] = date;
        }
    }

    public void AddTicker(string ticker, DateTime? lastSeen = null)
    {
        if (string.IsNullOrEmpty(ticker)) return;
        if (!_tickers.Contains(ticker)) _tickers.Add(ticker);
        if (lastSeen is not null && lastSeen.Value > _tickerLastSeen.GetValueOrDefault(ticker, DateTime.MinValue))
        {
            _tickerLastSeen[ticker] = lastSeen.Value;
        }
    }

    public void AddIsin(string isin, DateTime? lastSeen = null)
    {
        if (string.IsNullOrEmpty(isin)) return;
        if (!_isins.Contains(isin)) _isins.Add(isin);
        if (lastSeen is not null && lastSeen.Value > _isinLastSeen.GetValueOrDefault(isin, DateTime.MinValue))
        {
            _isinLastSeen[isin] = lastSeen.Value;
        }
    }

    public void AddFullName(string fullName, DateTime? lastSeen = null)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return;
        if (!_fullNames.Exists(name => string.Equals(name, fullName, StringComparison.OrdinalIgnoreCase)))
        {
            _fullNames.Add(fullName);
        }
        if (lastSeen is not null && lastSeen.Value > _fullNameLastSeen.GetValueOrDefault(fullName, DateTime.MinValue))
        {
            _fullNameLastSeen[fullName] = lastSeen.Value;
        }
    }

    /// <summary>
    /// Absorb another identity that turned out to describe the same share,
    /// e.g. after a manual link for a Newco insertion or when a shared ISIN is discovered.
    /// </summary>
    public void MergeFrom(ShareIdentity other)
    {
        if (ReferenceEquals(this, other)) return;
        foreach (string ticker in other._tickers)
        {
            AddTicker(ticker, other.GetTickerLastSeen(ticker));
        }
        foreach (string isin in other._isins) AddIsin(isin, other.GetIsinLastSeen(isin));
        foreach (string fullName in other._fullNames) AddFullName(fullName, other.GetFullNameLastSeen(fullName));
        foreach (((string ticker, string isin), DateTime lastSeen) in other._tickerIsinLastSeen)
        {
            if (lastSeen > _tickerIsinLastSeen.GetValueOrDefault((ticker, isin), DateTime.MinValue))
            {
                _tickerIsinLastSeen[(ticker, isin)] = lastSeen;
            }
        }
    }

    /// <summary>
    /// IBKR appends a lowercase exchange suffix to a symbol when the same instrument is traded on another
    /// exchange, e.g. "CWRl" vs "CWR" or "DISm" vs "DIS". Strip that suffix to get the base symbol.
    /// </summary>
    public static string StripExchangeSuffix(string symbol)
    {
        int end = symbol.Length;
        while (end > 0 && char.IsLower(symbol[end - 1]))
        {
            end--;
        }
        return symbol[..end];
    }
}

/// <summary>
/// A ticker/ISIN combination seen in the imported data and the date it was last seen. The ISIN is empty when the
/// event carried no ISIN (e.g. stock splits).
/// </summary>
public record ShareIdentityObservation(string Ticker, string Isin, DateTime LastSeen);

/// <summary>
/// A user declared link between two share references (a ticker or an ISIN each) stating that both refer to the
/// same share, used when the connection cannot be inferred from the imported data, e.g. a Newco insertion that
/// changed both the ticker and the ISIN. Persisted with the exported tax events.
/// </summary>
public record ShareIdentityLink(string Reference, string LinkedReference);
