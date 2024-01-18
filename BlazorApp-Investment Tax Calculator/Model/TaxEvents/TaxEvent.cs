using Model.Interfaces;

namespace Model.TaxEvents;

public abstract record TaxEvent : IAssetDatedEvent
{
    private static int _nextId = 0;
    public int Id { get; init; }
    public virtual required string AssetName { get; set; }
    public virtual required DateTime Date { get; set; }

    protected TaxEvent()
    {
        Id = Interlocked.Increment(ref _nextId);
    }
}
