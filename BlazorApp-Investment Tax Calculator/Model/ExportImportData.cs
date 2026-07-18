namespace InvestmentTaxCalculator.Model;

/// <summary>
/// Serialisation shape of the "Export data for future import" file. Extends the tax event lists with
/// user settings so the JSON stays a superset of the old format: files without the extra properties
/// still import, and old app versions ignore the properties they don't know.
/// </summary>
public record ExportImportData : TaxEventLists
{
    /// <summary>
    /// Residency status setting. Null when importing a file saved before this property existed.
    /// </summary>
    public List<ResidencyStatusRecord.RangeEntry>? ResidencyStatusRanges { get; set; }
}
