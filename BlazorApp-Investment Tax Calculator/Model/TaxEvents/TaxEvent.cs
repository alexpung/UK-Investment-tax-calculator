using InvestmentTaxCalculator.Model.Interfaces;

using System.Text.Json.Serialization;

namespace InvestmentTaxCalculator.Model.TaxEvents;

public abstract record TaxEvent : IAssetDatedEvent
{
    private static int _nextId = 0;
    [JsonIgnore]
    public int Id { get; private init; }
    public virtual required string AssetName { get; set; }
    public virtual required DateTime Date { get; set; }
    public string Isin { get; set; } = string.Empty;

    /// <summary>
    /// The share identity resolved for <see cref="AssetName"/> by the ShareIdentityRegistry, holding every known
    /// ticker variation and ISIN of the share. Null until the event is registered or when the asset is not a share.
    /// </summary>
    [JsonIgnore]
    public ShareIdentity? ShareIdentity { get; set; }

    /// <summary>
    /// The single name all ticker variations of this share resolve to, used for grouping and duplicate detection.
    /// Falls back to <see cref="AssetName"/> when no identity is attached or the asset name was changed after
    /// registration (e.g. the "Short " prefix given to short positions).
    /// </summary>
    [JsonIgnore]
    public string CanonicalAssetName =>
        ShareIdentity is not null && ShareIdentity.MatchesTicker(AssetName) ? ShareIdentity.PrimaryTicker : AssetName;

    /// <summary>
    /// Whether the given ticker refers to the same asset as this event, matching any recorded ticker variation of
    /// the share identity. Without an identity only the exact asset name matches.
    /// </summary>
    public bool IsSameAsset(string assetName)
    {
        if (ShareIdentity is not null && ShareIdentity.MatchesTicker(AssetName))
        {
            return ShareIdentity.MatchesTicker(assetName);
        }
        return string.Equals(AssetName, assetName, StringComparison.Ordinal);
    }

    protected TaxEvent()
    {
        Id = Interlocked.Increment(ref _nextId);
    }
    public virtual string GetDuplicateSignature()
    {
        return $"{CanonicalAssetName}|{Date.Ticks}|{Isin}";
    }

    public abstract string ToSummaryString();
}
