using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;

using System.Collections.Concurrent;
namespace InvestmentTaxCalculator.Model;


/// <summary>
/// Calculate tax results per year. Tax numbers are only rounded per year to the benefit of the tax payer to the nearest pound.
/// Calculations are not rounded.
/// https://www.gov.uk/hmrc-internal-manuals/self-assessment-manual/sam121370
/// </summary>
/// <param name="taxYear"></param>
public class TradeCalculationResult(ITaxYear taxYear)
{
    public ConcurrentBag<ITradeTaxCalculation> CalculatedTrade { get; set; } = [];
    public IEnumerable<ITradeTaxCalculation> GetDisposals => CalculatedTrade.Where(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL);

    public void Clear()
    {
        CalculatedTrade.Clear();
    }

    public void SetResult(List<ITradeTaxCalculation> tradeTaxCalculations)
    {
        foreach (var trade in tradeTaxCalculations)
        {
            CalculatedTrade.Add(trade);
        }
    }

    public bool IsTradeInSelectedTaxYear(IEnumerable<int> selectedYears, ITradeTaxCalculation taxCalculation)
    {
        return selectedYears.Contains(taxYear.ToTaxYear(taxCalculation.Date));
    }

    public int NumberOfDisposals(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        if (assetGroupType == AssetGroupType.ALL)
        {
            return CalculatedTrade.Count(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade) && trade.AcquisitionDisposal == TradeType.DISPOSAL);
        }
        return CalculatedTrade.Count(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade)
                                              && trade.AssetCategoryType.GetHmrcAssetCategoryType() == assetGroupType
                                              && trade.AcquisitionDisposal == TradeType.DISPOSAL);
    }

    public WrappedMoney DisposalProceeds(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        if (assetGroupType == AssetGroupType.ALL)
        {
            return DisposalProceeds(taxYearsFilter, AssetGroupType.LISTEDSHARES) + DisposalProceeds(taxYearsFilter, AssetGroupType.OTHERASSETS);
        }
        return CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade) && trade.AssetCategoryType.GetHmrcAssetCategoryType() == assetGroupType)
                          .Where(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL)
                          .GroupBy(trade => taxYear.ToTaxYear(trade.Date))
                          .Sum(group => group.Sum(trade => trade.TotalProceeds).Floor());
    }

    public WrappedMoney AllowableCosts(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        if (assetGroupType == AssetGroupType.ALL)
        {
            return AllowableCosts(taxYearsFilter, AssetGroupType.LISTEDSHARES) + AllowableCosts(taxYearsFilter, AssetGroupType.OTHERASSETS);
        }
        return CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade)
                                              && trade.AssetCategoryType.GetHmrcAssetCategoryType() == assetGroupType
                                              && trade.AcquisitionDisposal == TradeType.DISPOSAL)
                              .GroupBy(trade => taxYear.ToTaxYear(trade.Date))
                              .Sum(group => group.Sum(trade => trade.TotalAllowableCost).Ceiling());
    }

    public WrappedMoney TotalGain(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        if (assetGroupType == AssetGroupType.ALL)
        {
            return TotalGain(taxYearsFilter, AssetGroupType.LISTEDSHARES) + TotalGain(taxYearsFilter, AssetGroupType.OTHERASSETS);
        }
        return CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade)
                                              && trade.AssetCategoryType.GetHmrcAssetCategoryType() == assetGroupType
                                              && trade.AcquisitionDisposal == TradeType.DISPOSAL
                                              && trade.Gain.Amount > 0)
                              .GroupBy(trade => taxYear.ToTaxYear(trade.Date))
                              .Sum(group => group.Sum(trade => trade.Gain).Floor());
    }

    public WrappedMoney TotalLoss(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        if (assetGroupType == AssetGroupType.ALL)
        {
            return TotalLoss(taxYearsFilter, AssetGroupType.LISTEDSHARES) + TotalLoss(taxYearsFilter, AssetGroupType.OTHERASSETS);
        }
        return CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade)
                                              && trade.AssetCategoryType.GetHmrcAssetCategoryType() == assetGroupType
                                              && trade.AcquisitionDisposal == TradeType.DISPOSAL
                                              && trade.Gain.Amount < 0)
                              .GroupBy(trade => taxYear.ToTaxYear(trade.Date))
                              .Sum(group => group.Sum(trade => trade.Gain).Floor());
    }
}
