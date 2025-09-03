namespace InvestmentTaxCalculator.Model;

public class TaxYearCgtByTypeReport
{
    public int TaxYear { get; set; }
    public decimal ListedSecurityNumberOfDisposals { get; set; }
    public required WrappedMoney ListedSecurityDisposalProceeds { get; set; }
    public required WrappedMoney ListedSecurityAllowableCosts { get; set; }
    public required WrappedMoney ListedSecurityGainExcludeLoss { get; set; }
    public required WrappedMoney ListedSecurityLoss { get; set; }
    public decimal OtherAssetsNumberOfDisposals { get; set; }
    public required WrappedMoney OtherAssetsDisposalProceeds { get; set; }
    public required WrappedMoney OtherAssetsAllowableCosts { get; set; }
    public required WrappedMoney OtherAssetsGainExcludeLoss { get; set; }
    public required WrappedMoney OtherAssetsLoss { get; set; }
}
