using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Services;

public static class AssetNameNormaliser
{
    /// <summary>
    /// IBKR appends a lowercase exchange suffix to a symbol when the same instrument is traded on another exchange,
    /// e.g. "CWRl" vs "CWR" or "DISm" vs "DIS". When tax events sharing the same ISIN only differ by such a suffix
    /// they are the same instrument, so all of them are renamed to the common base symbol with the suffix discarded.
    /// The rename is also applied to events without an ISIN (e.g. stock splits) that use one of the affected symbols,
    /// so the whole imported data set refers to the instrument by a single name.
    /// </summary>
    public static void NormaliseAssetNamesByIsin(IEnumerable<TaxEvent> taxEvents)
    {
        List<TaxEvent> taxEventList = taxEvents as List<TaxEvent> ?? [.. taxEvents];
        Dictionary<string, string> renameMap = BuildRenameMap(taxEventList);
        if (renameMap.Count == 0) return;
        foreach (TaxEvent taxEvent in taxEventList)
        {
            if (renameMap.TryGetValue(taxEvent.AssetName, out string? baseSymbol))
            {
                taxEvent.AssetName = baseSymbol;
            }
        }
    }

    private static Dictionary<string, string> BuildRenameMap(IEnumerable<TaxEvent> taxEvents)
    {
        Dictionary<string, string> renameMap = [];
        IEnumerable<IGrouping<string, TaxEvent>> eventsGroupedByIsin = taxEvents.Where(taxEvent => !string.IsNullOrEmpty(taxEvent.Isin))
                                                                               .GroupBy(taxEvent => taxEvent.Isin);
        foreach (IGrouping<string, TaxEvent> isinGroup in eventsGroupedByIsin)
        {
            List<string> symbols = isinGroup.Select(taxEvent => taxEvent.AssetName).Distinct().ToList();
            if (symbols.Count < 2) continue;
            List<string> baseSymbols = symbols.Select(StripLowercaseSuffix).Distinct().ToList();
            if (baseSymbols.Count != 1 || string.IsNullOrEmpty(baseSymbols[0])) continue;
            foreach (string symbol in symbols.Where(symbol => symbol != baseSymbols[0]))
            {
                renameMap[symbol] = baseSymbols[0];
            }
        }
        return renameMap;
    }

    private static string StripLowercaseSuffix(string symbol)
    {
        int end = symbol.Length;
        while (end > 0 && char.IsLower(symbol[end - 1]))
        {
            end--;
        }
        return symbol[..end];
    }
}
