namespace InvestmentTaxCalculator.Model;

public record TaxYearCgtReport
{
    public int TaxYear { get; set; }
    public required WrappedMoney TotalGainInYear { get; set; }
    public required WrappedMoney TotalLossInYear { get; set; }
    public required WrappedMoney NetCapitalGain { get; set; }
    public required WrappedMoney CapitalGainAllowance { get; set; }
    public required WrappedMoney AvailableCapitalLossesFromPreviousYears { get; set; }
    public required WrappedMoney CgtAllowanceBroughtForwardAndUsed { get; set; }
    public required WrappedMoney TaxableGainAfterAllowanceAndLossOffset { get; set; }
    public required WrappedMoney LossesAvailableToBroughtForward { get; set; }
}
