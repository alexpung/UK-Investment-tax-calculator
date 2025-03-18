using InvestmentTaxCalculator.Enumerations;
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
            yield return CreateReport(taxYear);
        }
    }

    public TaxYearCgtByTypeReport GetTaxYearCgtByTypeReport(int taxYear)
    {
        return CreateReport(taxYear);
    }

    private TaxYearCgtByTypeReport CreateReport(int taxYear)
    {
        return new TaxYearCgtByTypeReport()
        {
            TaxYear = taxYear,
            ListedSecurityNumberOfDisposals = tradeCalculationResult.GetNumberOfDisposals([taxYear], AssetGroupType.LISTEDSHARES),
            ListedSecurityDisposalProceeds = tradeCalculationResult.GetDisposalProceeds([taxYear], AssetGroupType.LISTEDSHARES).Amount,
            ListedSecurityAllowableCosts = tradeCalculationResult.GetAllowableCosts([taxYear], AssetGroupType.LISTEDSHARES).Amount,
            ListedSecurityGainExcludeLoss = tradeCalculationResult.GetTotalGain([taxYear], AssetGroupType.LISTEDSHARES).Amount,
            ListedSecurityLoss = tradeCalculationResult.GetTotalLoss([taxYear], AssetGroupType.LISTEDSHARES).Amount,
            OtherAssetsNumberOfDisposals = tradeCalculationResult.GetNumberOfDisposals([taxYear], AssetGroupType.OTHERASSETS),
            OtherAssetsDisposalProceeds = tradeCalculationResult.GetDisposalProceeds([taxYear], AssetGroupType.OTHERASSETS).Amount,
            OtherAssetsAllowableCosts = tradeCalculationResult.GetAllowableCosts([taxYear], AssetGroupType.OTHERASSETS).Amount,
            OtherAssetsGainExcludeLoss = tradeCalculationResult.GetTotalGain([taxYear], AssetGroupType.OTHERASSETS).Amount,
            OtherAssetsLoss = tradeCalculationResult.GetTotalLoss([taxYear], AssetGroupType.OTHERASSETS).Amount
        };
    }
}
