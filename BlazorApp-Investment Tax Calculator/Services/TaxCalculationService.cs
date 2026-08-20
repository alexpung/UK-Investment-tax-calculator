using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

namespace InvestmentTaxCalculator.Services;

public enum CalculationTrigger
{
    Manual,
    NavigationRefresh
}

public class TaxCalculationService(
    UkSection104Pools section104Pools,
    IDividendCalculator dividendCalculator,
    DividendCalculationResult dividendCalculationResult,
    TradeCalculationResult tradeCalculationResult,
    IEnumerable<ITradeCalculator> tradeCalculators,
    YearOptions years,
    ITaxYear taxYear,
    ToastService toastService,
    TaxEventLists taxEventLists,
    ShareIdentityRegistry shareIdentityRegistry)
{
    private bool _isCalculating = false;
    public bool IsCalculating => _isCalculating;
    public CalculationTrigger CurrentTrigger { get; private set; } = CalculationTrigger.Manual;
    public event Action? OnStateChanged;

    public async Task CalculateAsync(CalculationTrigger trigger = CalculationTrigger.Manual)
    {
        if (_isCalculating) return;

        try
        {
            CurrentTrigger = trigger;
            _isCalculating = true;
            SafeInvokeOnStateChanged();
            section104Pools.Clear();
            tradeCalculationResult.Clear();
            ITradeTaxCalculation.ResetID();
            // Refresh share identities so events added since import (e.g. manual entries and corporate actions
            // entered in the UI) are matched by share identity during the calculation.
            shareIdentityRegistry.RegisterEvents(taxEventLists.AllEvents);

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
            CurrentTrigger = CalculationTrigger.Manual;
            SafeInvokeOnStateChanged();
        }
    }

    private void SafeInvokeOnStateChanged()
    {
        try
        {
            OnStateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TaxCalculationService.OnStateChanged subscriber failed: {ex}");
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
