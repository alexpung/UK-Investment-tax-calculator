using System.ComponentModel;

namespace InvestmentTaxCalculator.Enumerations;

public enum TaxableStatus
{
    [Description("Taxable")]
    TAXABLE,
    [Description("Non-Taxable")]
    NON_TAXABLE
}
