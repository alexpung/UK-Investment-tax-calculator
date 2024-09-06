using InvestmentTaxCalculator.Model.Interfaces;

namespace InvestmentTaxCalculator.Model.TaxEvents;

public abstract record TaxEvent : IAssetDatedEvent
{
    private static int _nextId = 0;
    public int Id { get; private init; }
    public virtual required string AssetName { get; set; }
    public virtual required DateTime Date { get; set; }

    protected TaxEvent()
    {
        Id = Interlocked.Increment(ref _nextId);
    }
}
