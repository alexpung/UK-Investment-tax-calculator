using System.ComponentModel;

namespace InvestmentTaxCalculator.Enumerations;

public enum ResidencyStatus
{
    [Description("UK Resident")]
    Resident,
    [Description("UK Non-Resident")]
    NonResident,
    [Description("UK Temporary Non-Residence")]
    TemporaryNonResident
}
