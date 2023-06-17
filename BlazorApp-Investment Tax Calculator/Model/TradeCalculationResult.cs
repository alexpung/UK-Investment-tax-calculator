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



    public decimal DisposalProceeds(IEnumerable<int> taxYearsFilter) => Math.Floor(CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade))
                                                                                                   .Where(trade => trade.BuySell == Enum.TradeType.SELL)
                                                                                                   .Sum(trade => trade.TotalProceeds));

    public decimal AllowableCosts(IEnumerable<int> taxYearsFilter) => Math.Ceiling(CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade))
                                                                                                   .Where(trade => trade.BuySell == Enum.TradeType.SELL)
                                                                                                   .Sum(trade => trade.TotalAllowableCost));

    public decimal TotalGain(IEnumerable<int> taxYearsFilter) => Math.Floor(CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade))
                                                                                            .Where(trade => trade.BuySell == Enum.TradeType.SELL)
                                                                                            .Where(trade => trade.Gain > 0)
                                                                                            .Sum(trade => trade.Gain));

    public decimal TotalLoss(IEnumerable<int> taxYearsFilter) => Math.Ceiling(CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade))
                                                                                              .Where(trade => trade.BuySell == Enum.TradeType.SELL)
                                                                                              .Where(trade => trade.Gain < 0)
                                                                                              .Sum(trade => trade.Gain));
}
