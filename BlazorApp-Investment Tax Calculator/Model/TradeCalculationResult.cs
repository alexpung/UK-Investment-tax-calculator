using Enumerations;

using Model.Interfaces;

using System.Collections.Concurrent;
namespace Model;


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

    // Rounding to tax payer benefit https://www.gov.uk/hmrc-internal-manuals/self-assessment-manual/sam121370
    public int NumberOfDisposals(IEnumerable<int> taxYearsFilter) => CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade))
                                                                                    .Count(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL);



    public WrappedMoney DisposalProceeds(IEnumerable<int> taxYearsFilter) => CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade))
                                                                                                   .Where(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL)
                                                                                                   .Sum(trade => trade.TotalProceeds)
                                                                                                   .Floor();

    public WrappedMoney AllowableCosts(IEnumerable<int> taxYearsFilter) => CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade))
                                                                                                   .Where(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL)
                                                                                                   .Sum(trade => trade.TotalAllowableCost)
                                                                                                   .Ceiling();

    public WrappedMoney TotalGain(IEnumerable<int> taxYearsFilter) => CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade))
                                                                                            .Where(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL)
                                                                                            .Where(trade => trade.Gain.Amount > 0)
                                                                                            .Sum(trade => trade.Gain)
                                                                                            .Floor();

    public WrappedMoney TotalLoss(IEnumerable<int> taxYearsFilter) => CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade))
                                                                                              .Where(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL)
                                                                                              .Where(trade => trade.Gain.Amount < 0)
                                                                                              .Sum(trade => trade.Gain)
                                                                                              .Ceiling();
}
