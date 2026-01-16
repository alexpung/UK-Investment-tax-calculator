using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

namespace InvestmentTaxCalculator.Services;

public class TaxCalculationService(
    UkSection104Pools section104Pools,
    IDividendCalculator dividendCalculator,
    DividendCalculationResult dividendCalculationResult,
    TradeCalculationResult tradeCalculationResult,
    IEnumerable<ITradeCalculator> tradeCalculators,
    YearOptions years,
    ITaxYear taxYear,
    ToastService toastService)
{
    private bool _isCalculating = false;
    public bool IsCalculating => _isCalculating;

    public async Task CalculateAsync()
    {
        if (_isCalculating) return;

        try
        {
            _isCalculating = true;
            section104Pools.Clear();
            tradeCalculationResult.Clear();
            TradeTaxCalculation.ResetID();

            foreach (ITradeCalculator tradeCalculator in tradeCalculators)
            {
                tradeCalculationResult.SetResult(await Task.Run(tradeCalculator.CalculateTax));
            }

            dividendCalculationResult.SetResult(await Task.Run(dividendCalculator.CalculateTax));
            years.SetYears(GetSelectableYears());

            toastService.ShowInformation("Calculation completed.");
        }
        catch (Exception ex)
        {
            toastService.ShowException(ex);
        }
        finally
        {
            _isCalculating = false;
        }
    }

    private IEnumerable<int> GetSelectableYears()
    {
        IEnumerable<int> taxYearsWithDisposal = tradeCalculationResult.CalculatedTrade
            .Where(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL)
            .Select(trade => taxYear.ToTaxYear(trade.Date))
            .Distinct();

        IEnumerable<int> taxYearsWithDividend = dividendCalculationResult.DividendSummary
            .Select(dividend => dividend.TaxYear)
            .Distinct();

        return taxYearsWithDisposal.Union(taxYearsWithDividend).OrderByDescending(i => i);
    }
}
