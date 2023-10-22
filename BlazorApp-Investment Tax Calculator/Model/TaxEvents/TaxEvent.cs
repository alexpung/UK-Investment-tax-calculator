using Model.Interfaces;

namespace Model.TaxEvents;

public abstract record TaxEvent : IAssetDatedEvent
{
    public virtual required string AssetName { get; set; }
    public virtual required DateTime Date { get; set; }
}
