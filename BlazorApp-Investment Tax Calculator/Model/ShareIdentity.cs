namespace InvestmentTaxCalculator.Model;

/// <summary>
/// Records the identity of a share independently of any single ticker string.
/// A share keeps its identity through ticker renames, Newco insertions (which change the ISIN and possibly the
/// ticker) and exchange suffix variations (IBKR appends a lowercase exchange suffix, e.g. "CWRl" vs "CWR"),
/// so all recorded ISINs and ticker variations refer to the same instrument for tax matching purposes.
/// </summary>
public class ShareIdentity
{
    private readonly List<string> _tickers = [];
    private readonly List<string> _isins = [];
    private readonly List<string> _fullNames = [];
    private readonly Dictionary<string, DateTime> _tickerLastSeen = new(StringComparer.Ordinal);

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

    public bool MatchesTicker(string ticker) =>
        !string.IsNullOrEmpty(ticker) && (_tickers.Contains(ticker) || ticker == PrimaryTicker);

    public bool MatchesIsin(string isin) => !string.IsNullOrEmpty(isin) && _isins.Contains(isin);

    public bool IsSameShare(ShareIdentity other) =>
        ReferenceEquals(this, other) || other._tickers.Exists(_tickers.Contains) || other._isins.Exists(_isins.Contains);

    public void AddTicker(string ticker, DateTime? lastSeen = null)
    {
        if (string.IsNullOrEmpty(ticker)) return;
        if (!_tickers.Contains(ticker)) _tickers.Add(ticker);
        if (lastSeen is not null && lastSeen.Value > _tickerLastSeen.GetValueOrDefault(ticker, DateTime.MinValue))
        {
            _tickerLastSeen[ticker] = lastSeen.Value;
        }
    }

    public void AddIsin(string isin)
    {
        if (string.IsNullOrEmpty(isin)) return;
        if (!_isins.Contains(isin)) _isins.Add(isin);
    }

    public void AddFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return;
        if (!_fullNames.Exists(name => string.Equals(name, fullName, StringComparison.OrdinalIgnoreCase)))
        {
            _fullNames.Add(fullName);
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
            AddTicker(ticker, other._tickerLastSeen.TryGetValue(ticker, out DateTime lastSeen) ? lastSeen : null);
        }
        foreach (string isin in other._isins) AddIsin(isin);
        foreach (string fullName in other._fullNames) AddFullName(fullName);
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
/// A user declared link between two share references (a ticker or an ISIN each) stating that both refer to the
/// same share, used when the connection cannot be inferred from the imported data, e.g. a Newco insertion that
/// changed both the ticker and the ISIN. Persisted with the exported tax events.
/// </summary>
public record ShareIdentityLink(string Reference, string LinkedReference);
