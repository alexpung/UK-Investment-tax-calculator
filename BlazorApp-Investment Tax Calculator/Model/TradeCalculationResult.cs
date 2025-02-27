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
    private readonly ConcurrentBag<ITradeTaxCalculation> _calculatedTrade = [];
    public ConcurrentBag<ITradeTaxCalculation> CalculatedTrade => _calculatedTrade;
    public IEnumerable<ITradeTaxCalculation> GetDisposals => _calculatedTrade.Where(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL);
    public ConcurrentDictionary<(int, AssetGroupType), List<ITradeTaxCalculation>> TradeByYear { get; } = new();
    public ConcurrentDictionary<(int, AssetGroupType), List<ITradeTaxCalculation>> DisposalByYear { get; } = new();
    private readonly ConcurrentDictionary<(int, AssetGroupType), int> _numberOfDisposals = new();
    private readonly ConcurrentDictionary<(int, AssetGroupType), WrappedMoney> _disposalProceeds = new();
    private readonly ConcurrentDictionary<(int, AssetGroupType), WrappedMoney> _allowableCosts = new();
    private readonly ConcurrentDictionary<(int, AssetGroupType), WrappedMoney> _totalGain = new();
    private readonly ConcurrentDictionary<(int, AssetGroupType), WrappedMoney> _totalLoss = new();

    public void Clear()
    {
        _calculatedTrade.Clear();
        DisposalByYear.Clear();
        TradeByYear.Clear();
        _numberOfDisposals.Clear();
        _disposalProceeds.Clear();
        _allowableCosts.Clear();
        _totalGain.Clear();
        _totalLoss.Clear();
    }

    public void SetResult(List<ITradeTaxCalculation> tradeTaxCalculations)
    {
        foreach (var trade in tradeTaxCalculations)
        {
            _calculatedTrade.Add(trade);
        }
        IEnumerable<IGrouping<(int, AssetGroupType), ITradeTaxCalculation>> groupedTradeByYear = _calculatedTrade
            .GroupBy(trade => (taxYear.ToTaxYear(trade.Date), trade.AssetCategoryType.GetHmrcAssetCategoryType()));
        foreach (var group in groupedTradeByYear)
        {
            TradeByYear[group.Key] = [.. group];
            DisposalByYear[group.Key] = [.. group.Where(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL)];
            _numberOfDisposals[group.Key] = DisposalByYear[group.Key].Count;
            _disposalProceeds[group.Key] = DisposalByYear[group.Key].Sum(trade => trade.TotalProceeds).Floor();
            _allowableCosts[group.Key] = DisposalByYear[group.Key].Sum(trade => trade.TotalAllowableCost).Ceiling();
            _totalGain[group.Key] = DisposalByYear[group.Key].Where(trade => trade.Gain.Amount > 0).Sum(trade => trade.Gain).Floor();
            _totalLoss[group.Key] = DisposalByYear[group.Key].Where(trade => trade.Gain.Amount < 0).Sum(trade => trade.Gain).Floor();
        }
    }

    public int NumberOfDisposals(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        int result = 0;
        foreach (int year in taxYearsFilter)
        {
            if (assetGroupType == AssetGroupType.ALL)
            {
                result += _numberOfDisposals.TryGetValue((year, AssetGroupType.LISTEDSHARES), out int result1) ? result1 : 0;
                result += _numberOfDisposals.TryGetValue((year, AssetGroupType.OTHERASSETS), out int result2) ? result2 : 0;
            }
            else
            {
                result += _numberOfDisposals.TryGetValue((year, assetGroupType), out int result1) ? result1 : 0;
            }
        }
        return result;
    }

    public WrappedMoney DisposalProceeds(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        return GetStats(taxYearsFilter, assetGroupType, _disposalProceeds);
    }

    public WrappedMoney AllowableCosts(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        return GetStats(taxYearsFilter, assetGroupType, _allowableCosts);
    }

    public WrappedMoney TotalGain(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        return GetStats(taxYearsFilter, assetGroupType, _totalGain);
    }

    public WrappedMoney TotalLoss(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        return GetStats(taxYearsFilter, assetGroupType, _totalLoss);
    }

    private static WrappedMoney GetStats(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType, ConcurrentDictionary<(int, AssetGroupType), WrappedMoney> data)
    {
        WrappedMoney result = WrappedMoney.GetBaseCurrencyZero();
        foreach (int year in taxYearsFilter)
        {
            if (assetGroupType == AssetGroupType.ALL)
            {
                result += data.TryGetValue((year, AssetGroupType.LISTEDSHARES), out WrappedMoney? result1) ? result1 : WrappedMoney.GetBaseCurrencyZero();
                result += data.TryGetValue((year, AssetGroupType.OTHERASSETS), out WrappedMoney? result2) ? result2 : WrappedMoney.GetBaseCurrencyZero();
            }
            else
            {
                result += data.TryGetValue((year, assetGroupType), out WrappedMoney? result1) ? result1 : WrappedMoney.GetBaseCurrencyZero();
            }
        }
        return result;
    }
}
