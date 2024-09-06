using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;

using System.Collections.Concurrent;
namespace InvestmentTaxCalculator.Model;


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

    private bool IsTradeInSelectedTaxYear(IEnumerable<int> selectedYears, ITradeTaxCalculation taxCalculation)
    {
        return selectedYears.Contains(taxYear.ToTaxYear(taxCalculation.Date));
    }

    private static bool FilterAssetType(ITradeTaxCalculation trade, AssetGroupType assetGroupType) => assetGroupType switch
    {
        AssetGroupType.ALL => true,
        AssetGroupType.LISTEDSHARES => trade.AssetCatagoryType is AssetCatagoryType.STOCK,
        AssetGroupType.OTHERASSETS => trade.AssetCatagoryType is AssetCatagoryType.FUTURE or AssetCatagoryType.FX,
        _ => throw new NotImplementedException()
    };

    // Rounding to tax payer benefit https://www.gov.uk/hmrc-internal-manuals/self-assessment-manual/sam121370
    public int NumberOfDisposals(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        return CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade) && FilterAssetType(trade, assetGroupType))
                              .Count(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL);
    }

    public WrappedMoney DisposalProceeds(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        return CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade) && FilterAssetType(trade, assetGroupType))
                              .Where(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL)
                              .Sum(trade => trade.TotalProceeds)
                              .Floor();
    }

    public WrappedMoney AllowableCosts(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        return CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade) && FilterAssetType(trade, assetGroupType))
                              .Where(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL)
                              .Sum(trade => trade.TotalAllowableCost)
                              .Ceiling();
    }

    public WrappedMoney TotalGain(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        return CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade) && FilterAssetType(trade, assetGroupType))
                              .Where(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL)
                              .Where(trade => trade.Gain.Amount > 0)
                              .Sum(trade => trade.Gain)
                              .Floor();
    }

    public WrappedMoney TotalLoss(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        return CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade) && FilterAssetType(trade, assetGroupType))
                              .Where(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL)
                              .Where(trade => trade.Gain.Amount < 0)
                              .Sum(trade => trade.Gain)
                              .Ceiling();
    }
}

public enum AssetGroupType
{
    ALL,
    LISTEDSHARES,
    OTHERASSETS,
}
