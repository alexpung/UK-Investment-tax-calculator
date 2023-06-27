namespace Model;

public abstract record TaxEvent
{
    public required string AssetName { get; set; }
    public required DateTime Date { get; set; }
}
