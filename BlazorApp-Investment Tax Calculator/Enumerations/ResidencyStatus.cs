using System.ComponentModel;

namespace InvestmentTaxCalculator.Enumerations;

public enum ResidencyStatus
{
    [Description("Resident for tax purposes")]
    Resident,
    [Description("Non-resident for tax purposes")]
    NonResident,
    [Description("Temporary Non-Residence for tax purposes")]
    TemporaryNonResident
}
