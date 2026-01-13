using System.ComponentModel;

namespace InvestmentTaxCalculator.Enumerations;

public enum DividendType
{
    [Description("Withholding Tax")]
    WITHHOLDING,
    [Description("Dividend")]
    DIVIDEND,
    [Description("Payment in lieu of dividend")]
    DIVIDEND_IN_LIEU,
    [Description("Not a dividend")]
    NOT_DIVIDEND,
    [Description("Excess Reportable Income (Dividend)")]
    EXCESS_REPORTABLE_INCOME
}
