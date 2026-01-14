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

    protected TaxEvent()
    {
        Id = Interlocked.Increment(ref _nextId);
    }
}
