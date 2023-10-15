namespace Model.TaxEvents;

public abstract record TaxEvent
{
    public virtual required string AssetName { get; set; }
    public virtual required DateTime Date { get; set; }
}
