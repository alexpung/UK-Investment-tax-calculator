using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;

namespace InvestmentTaxCalculator.Services;

public class TaxYearCgtByTypeReportService(TradeCalculationResult tradeCalculationResult, ITaxYear taxYearConverter)
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IEnumerable<TaxYearCgtByTypeReport> GetTaxYearCgtByTypeReports()
    {
        List<int> taxYears = [.. tradeCalculationResult.CalculatedTrade.Select(trade => taxYearConverter.ToTaxYear(trade.Date))
                                                                                                        .Distinct()
                                                                                                        .Order()];
        foreach (int taxYear in taxYears)
        {
            yield return new TaxYearCgtByTypeReport()
            {
                TaxYear = taxYear,
                ListedSecurityNumberOfDisposals = tradeCalculationResult.NumberOfDisposals([taxYear], AssetGroupType.LISTEDSHARES),
                ListedSecurityDisposalProceeds = tradeCalculationResult.DisposalProceeds([taxYear], AssetGroupType.LISTEDSHARES).Amount,
                ListedSecurityAllowableCosts = tradeCalculationResult.AllowableCosts([taxYear], AssetGroupType.LISTEDSHARES).Amount,
                ListedSecurityGainExcludeLoss = tradeCalculationResult.TotalGain([taxYear], AssetGroupType.LISTEDSHARES).Amount,
                ListedSecurityLoss = tradeCalculationResult.TotalLoss([taxYear], AssetGroupType.LISTEDSHARES).Amount,
                OtherAssetsNumberOfDisposals = tradeCalculationResult.NumberOfDisposals([taxYear], AssetGroupType.OTHERASSETS),
                OtherAssetsDisposalProceeds = tradeCalculationResult.DisposalProceeds([taxYear], AssetGroupType.OTHERASSETS).Amount,
                OtherAssetsAllowableCosts = tradeCalculationResult.AllowableCosts([taxYear], AssetGroupType.OTHERASSETS).Amount,
                OtherAssetsGainExcludeLoss = tradeCalculationResult.TotalGain([taxYear], AssetGroupType.OTHERASSETS).Amount,
                OtherAssetsLoss = tradeCalculationResult.TotalLoss([taxYear], AssetGroupType.OTHERASSETS).Amount
            };
        }
    }

    public TaxYearCgtByTypeReport GetTaxYearCgtByTypeReport(int taxYear)
    {
        return new TaxYearCgtByTypeReport()
        {
            TaxYear = taxYear,
            ListedSecurityNumberOfDisposals = tradeCalculationResult.NumberOfDisposals([taxYear], AssetGroupType.LISTEDSHARES),
            ListedSecurityDisposalProceeds = tradeCalculationResult.DisposalProceeds([taxYear], AssetGroupType.LISTEDSHARES).Amount,
            ListedSecurityAllowableCosts = tradeCalculationResult.AllowableCosts([taxYear], AssetGroupType.LISTEDSHARES).Amount,
            ListedSecurityGainExcludeLoss = tradeCalculationResult.TotalGain([taxYear], AssetGroupType.LISTEDSHARES).Amount,
            ListedSecurityLoss = tradeCalculationResult.TotalLoss([taxYear], AssetGroupType.LISTEDSHARES).Amount,
            OtherAssetsNumberOfDisposals = tradeCalculationResult.NumberOfDisposals([taxYear], AssetGroupType.OTHERASSETS),
            OtherAssetsDisposalProceeds = tradeCalculationResult.DisposalProceeds([taxYear], AssetGroupType.OTHERASSETS).Amount,
            OtherAssetsAllowableCosts = tradeCalculationResult.AllowableCosts([taxYear], AssetGroupType.OTHERASSETS).Amount,
            OtherAssetsGainExcludeLoss = tradeCalculationResult.TotalGain([taxYear], AssetGroupType.OTHERASSETS).Amount,
            OtherAssetsLoss = tradeCalculationResult.TotalLoss([taxYear], AssetGroupType.OTHERASSETS).Amount
        };
    }
}
