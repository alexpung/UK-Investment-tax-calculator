using Model;

using System.Text.Json;

namespace Services;

public class ExportTaxEventService(TaxEventLists taxEventLists)
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    public string SerialiseTaxEvents()
    {
        return JsonSerializer.Serialize(taxEventLists, _options);
    }
}
