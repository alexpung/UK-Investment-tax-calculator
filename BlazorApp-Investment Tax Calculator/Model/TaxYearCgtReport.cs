namespace InvestmentTaxCalculator.Model;

public record TaxYearCgtReport
{
    public int TaxYear { get; set; }
    public decimal TotalGainInYear { get; set; }
    public decimal TotalLossInYear { get; set; }
    public decimal NetCapitalGain { get; set; }
    public decimal CapitalGainAllowance { get; set; }
    public decimal AvailableCapitalLossesFromPreviousYears { get; set; }
    public decimal CgtAllowanceBroughtForwardAndUsed { get; set; }
    public decimal TaxableGainAfterAllowanceAndLossOffset { get; set; }
    public decimal LossesAvailableToBroughtForward { get; set; }
}
