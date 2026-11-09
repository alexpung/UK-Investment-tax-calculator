namespace InvestmentTaxCalculator.Model;

/// <summary>
/// Serialisation shape of the "Export data for future import" file. Extends the tax event lists with
/// user settings so the JSON stays a superset of the old format: files without the extra properties
/// still import, and old app versions ignore the properties they don't know.
/// </summary>
public record ExportImportData : TaxEventLists
{
    /// <summary>
    /// The format version written by this app version. Version 2: ERI income and fund equalisation reductions are
    /// derived at calculation time rather than stored on the income events.
    /// </summary>
    public const int CurrentFormatVersion = 2;

    /// <summary>
    /// The format of the file. Null for files saved before versioning, which store ERI income and equalisation
    /// reductions on the income events and are converted on import (see ReportingFundLegacyMigration).
    /// </summary>
    public int? FormatVersion { get; set; }

    /// <summary>
    /// Residency status setting. Null when importing a file saved before this property existed.
    /// </summary>
    public List<ResidencyStatusRecord.RangeEntry>? ResidencyStatusRanges { get; set; }

    /// <summary>
    /// User declared links between share references (ticker or ISIN) that refer to the same share, e.g. for a
    /// Newco insertion. Null when importing a file saved before this property existed.
    /// </summary>
    public List<ShareIdentityLink>? ShareIdentityLinks { get; set; }
}
