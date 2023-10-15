namespace Model.TaxEvents;

public record StockSplit : CorporateAction
{
    public required int NumberBeforeSplit { get; set; }
    public required int NumberAfterSplit { get; set; }
    public bool Rounding { get; set; } = true;

    public decimal GetSharesAfterSplit(decimal quantity)
    {
        decimal result = quantity * NumberAfterSplit / NumberBeforeSplit;
        return Rounding ? Math.Round(result, MidpointRounding.ToZero) : result;
    }
}
