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
public class TradeCalculationResult(ITaxYear taxYear, ResidencyStatusRecord residencyStatusRecord)
{
    private readonly ConcurrentBag<ITradeTaxCalculation> _calculatedTrade = [];
    public ConcurrentBag<ITradeTaxCalculation> CalculatedTrade => _calculatedTrade;
    public ConcurrentDictionary<(int, AssetCategoryType), List<ITradeTaxCalculation>> TradeByYear { get; } = new();
    public ConcurrentDictionary<(int, AssetCategoryType), List<ITradeTaxCalculation>> DisposalByYear { get; } = new();
    public ConcurrentDictionary<(int, AssetCategoryType), int> NumberOfDisposals => _numberOfDisposals;
    public ConcurrentDictionary<(int, AssetCategoryType), WrappedMoney> DisposalProceeds => _disposalProceeds;
    public ConcurrentDictionary<(int, AssetCategoryType), WrappedMoney> AllowableCosts => _allowableCosts;
    public ConcurrentDictionary<(int, AssetCategoryType), WrappedMoney> TotalGain => _totalGain;
    public ConcurrentDictionary<(int, AssetCategoryType), WrappedMoney> TotalLoss => _totalLoss;

    private readonly ConcurrentDictionary<(int, AssetCategoryType), int> _numberOfDisposals = new();
    private readonly ConcurrentDictionary<(int, AssetCategoryType), WrappedMoney> _disposalProceeds = new();
    private readonly ConcurrentDictionary<(int, AssetCategoryType), WrappedMoney> _allowableCosts = new();
    private readonly ConcurrentDictionary<(int, AssetCategoryType), WrappedMoney> _totalGain = new();
    private readonly ConcurrentDictionary<(int, AssetCategoryType), WrappedMoney> _totalLoss = new();

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
            if (trade.ResidencyStatusAtTrade == ResidencyStatus.TemporaryNonResident)
            {
                DateTime endDate = residencyStatusRecord.GetResidencyStatusPeriodEnd(trade.Date);
                if (endDate <= DateTime.MaxValue.AddDays(-1))
                {
                    trade.TaxableDate = endDate.AddDays(1);
                }
                else
                {
                    trade.TaxableDate = endDate;
                }
            }
            _calculatedTrade.Add(trade);
        }

        IEnumerable<IGrouping<(int, AssetCategoryType), ITradeTaxCalculation>> groupedTradeByYear = _calculatedTrade
            .GroupBy(trade =>
            {
                var taxableDate = trade.ResidencyStatusAtTrade == ResidencyStatus.TemporaryNonResident
                    ? trade.TaxableDate
                    : trade.Date;
                return (taxYear.ToTaxYear(taxableDate), trade.AssetCategoryType);
            });

        foreach (var group in groupedTradeByYear)
        {
            TradeByYear[group.Key] = [.. group];
            DisposalByYear[group.Key] = [.. group.Where(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL && trade.MatchHistory.Exists(match => match.IsTaxable is TaxableStatus.TAXABLE))];
            _numberOfDisposals[group.Key] = DisposalByYear[group.Key].Count;
            _disposalProceeds[group.Key] = DisposalByYear[group.Key].Sum(trade => trade.TotalProceeds).Floor();
            _allowableCosts[group.Key] = DisposalByYear[group.Key].Sum(trade => trade.TotalAllowableCost).Ceiling();
            _totalGain[group.Key] = DisposalByYear[group.Key].Where(trade => trade.Gain.Amount > 0).Sum(trade => trade.Gain).Floor();
            _totalLoss[group.Key] = DisposalByYear[group.Key].Where(trade => trade.Gain.Amount < 0).Sum(trade => trade.Gain).Floor();
        }
    }

    public int GetNumberOfDisposals(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        if (assetGroupType == AssetGroupType.ALL)
        {
            return DisposalByYear.Where(group => taxYearsFilter.Contains(group.Key.Item1))
                                 .Sum(group => group.Value.Count);
        }
        else
        {
            return DisposalByYear.Where(group => taxYearsFilter.Contains(group.Key.Item1) && group.Key.Item2.GetHmrcAssetCategoryType() == assetGroupType)
                                 .Sum(group => group.Value.Count);
        }
    }

    public WrappedMoney GetDisposalProceeds(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        return GetStats(taxYearsFilter, assetGroupType, _disposalProceeds);
    }

    public WrappedMoney GetAllowableCosts(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        return GetStats(taxYearsFilter, assetGroupType, _allowableCosts);
    }

    public WrappedMoney GetTotalGain(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        return GetStats(taxYearsFilter, assetGroupType, _totalGain);
    }

    public WrappedMoney GetTotalLoss(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType = AssetGroupType.ALL)
    {
        return GetStats(taxYearsFilter, assetGroupType, _totalLoss);
    }

    private static WrappedMoney GetStats(IEnumerable<int> taxYearsFilter, AssetGroupType assetGroupType, ConcurrentDictionary<(int, AssetCategoryType), WrappedMoney> data)
    {
        WrappedMoney result = WrappedMoney.GetBaseCurrencyZero();
        foreach (int year in taxYearsFilter)
        {
            if (assetGroupType == AssetGroupType.ALL)
            {
                foreach (var group in data)
                {
                    if (group.Key.Item1 == year)
                    {
                        result += group.Value;
                    }
                }
            }
            else
            {
                foreach (var group in data)
                {
                    if (group.Key.Item1 == year && group.Key.Item2.GetHmrcAssetCategoryType() == assetGroupType)
                    {
                        result += group.Value;
                    }
                }
            }
        }
        return result;
    }
}
