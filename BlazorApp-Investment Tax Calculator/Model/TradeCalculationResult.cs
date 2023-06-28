using Model.Interfaces;
using NMoneys;

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



    public Money DisposalProceeds(IEnumerable<int> taxYearsFilter) => CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade))
                                                                                                   .Where(trade => trade.BuySell == Enum.TradeType.SELL)
                                                                                                   .BaseCurrencySum(trade => trade.TotalProceeds)
                                                                                                   .Floor();

    public Money AllowableCosts(IEnumerable<int> taxYearsFilter) => CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade))
                                                                                                   .Where(trade => trade.BuySell == Enum.TradeType.SELL)
                                                                                                   .BaseCurrencySum(trade => trade.TotalAllowableCost)
                                                                                                   .Ceiling();

    public Money TotalGain(IEnumerable<int> taxYearsFilter) => CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade))
                                                                                            .Where(trade => trade.BuySell == Enum.TradeType.SELL)
                                                                                            .Where(trade => trade.Gain.Amount > 0)
                                                                                            .BaseCurrencySum(trade => trade.Gain)
                                                                                            .Floor();

    public Money TotalLoss(IEnumerable<int> taxYearsFilter) => CalculatedTrade.Where(trade => IsTradeInSelectedTaxYear(taxYearsFilter, trade))
                                                                                              .Where(trade => trade.BuySell == Enum.TradeType.SELL)
                                                                                              .Where(trade => trade.Gain.Amount < 0)
                                                                                              .BaseCurrencySum(trade => trade.Gain)
                                                                                              .Ceiling();
}
