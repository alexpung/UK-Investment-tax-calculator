using System.Text.Json.Serialization;

namespace Model.TaxEvents;

[JsonDerivedType(typeof(StockSplit), "stockSplit")]
public abstract record CorporateAction : TaxEvent
{
}
