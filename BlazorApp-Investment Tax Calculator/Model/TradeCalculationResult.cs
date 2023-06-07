using Model.Interfaces;

namespace Model;

public class TradeCalculationResult
{
    private readonly ITaxYear _taxYear;

    public TradeCalculationResult(ITaxYear taxYear)
    {
        _taxYear = taxYear;
    }

    public List<ITradeTaxCalculation> CalculatedTrade { get; set; } = new();

    public void SetResult(List<ITradeTaxCalculation> tradeTaxCalculations)
    {
        CalculatedTrade = tradeTaxCalculations;
    }

    private bool IsTradeInSelectedTaxYear(IEnumerable<int> selectedYears, ITradeTaxCalculation taxCalculation)
    {
        return selectedYears.Contains(_taxYear.ToTaxYear(taxCalculation.Date));
    }

    // Rounding to tax payer benefit https://www.gov.uk/hmrc-internal-manuals/self-assessment-manual/sam121370
    public int NumberOfDisposals(IEnumerable<int> taxYearsFilter) => CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade))
                                                                                    .Count(trade => trade.BuySell == Enum.TradeType.SELL);



    public int DisposalProceeds(IEnumerable<int> taxYearsFilter) => (int)Math.Floor(CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade))
                                                                                                   .Where(trade => trade.BuySell == Enum.TradeType.SELL)
                                                                                                   .Sum(trade => trade.TotalProceeds));

    public int AllowableCosts(IEnumerable<int> taxYearsFilter) => (int)Math.Ceiling(CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade))
                                                                                                   .Where(trade => trade.BuySell == Enum.TradeType.SELL)
                                                                                                   .Sum(trade => trade.TotalAllowableCost));

    public int TotalGain(IEnumerable<int> taxYearsFilter) => (int)Math.Floor(CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade))
                                                                                            .Where(trade => trade.BuySell == Enum.TradeType.SELL)
                                                                                            .Where(trade => trade.Gain > 0)
                                                                                            .Sum(trade => trade.Gain));

    public int TotalLoss(IEnumerable<int> taxYearsFilter) => (int)Math.Ceiling(CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade))
                                                                                              .Where(trade => trade.BuySell == Enum.TradeType.SELL)
                                                                                              .Where(trade => trade.Gain < 0)
                                                                                              .Sum(trade => trade.Gain));
}
