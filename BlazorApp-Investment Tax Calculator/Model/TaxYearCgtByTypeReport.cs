namespace Model;

public class TaxYearCgtByTypeReport
{
    public int TaxYear { get; set; }
    public decimal ListedSecurityNumberOfDisposals { get; set; }
    public decimal ListedSecurityDisposalProceeds { get; set; }
    public decimal ListedSecurityAllowableCosts { get; set; }
    public decimal ListedSecurityGainExcludeLoss { get; set; }
    public decimal ListedSecurityLoss { get; set; }
    public decimal OtherAssetsNumberOfDisposals { get; set; }
    public decimal OtherAssetsDisposalProceeds { get; set; }
    public decimal OtherAssetsAllowableCosts { get; set; }
    public decimal OtherAssetsGainExcludeLoss { get; set; }
    public decimal OtherAssetsLoss { get; set; }
}
