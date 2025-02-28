namespace InvestmentTaxCalculator.Components;
using Enumerations;

using InvestmentTaxCalculator.Services;

using Microsoft.AspNetCore.Components;

using Model;
using Model.Interfaces;
using Model.UkTaxModel;

public partial class StartCalculation
{
    [Inject] public required UkSection104Pools Section104Pools { get; set; }
    [Inject] public required IDividendCalculator DividendCalculator { get; set; }
    [Inject] public required DividendCalculationResult DividendCalculationResult { get; set; }
    [Inject] public required TradeCalculationResult TradeCalculationResult { get; set; }
    [Inject] public required IEnumerable<ITradeCalculator> TradeCalculators { get; set; }
    [Inject] public required YearOptions Years { get; set; }
    [Inject] public required ITaxYear TaxYear { get; set; }
    [Inject] public required ToastService ToastService { get; set; }

    private bool _isCalculating = false;

    public async Task OnStartCalculation()
    {
        try
        {
            _isCalculating = true;
            Section104Pools.Clear();
            TradeCalculationResult.Clear();
            foreach (ITradeCalculator tradeCalculator in TradeCalculators)
            {
                TradeCalculationResult.SetResult(await Task.Run(tradeCalculator.CalculateTax));
            }
            DividendCalculationResult.SetResult(await Task.Run(DividendCalculator.CalculateTax));
            Years.SetYears(GetSelectableYears());
            StateHasChanged();
            ToastService.ShowInformation("Calculation completed.");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Calculation failed: {ex.Message}");
        }
        finally
        {
            _isCalculating = false;
        }
    }

    private IEnumerable<int> GetSelectableYears()
    {
        IEnumerable<int> taxYearsWithDisposal = TradeCalculationResult.CalculatedTrade.Where(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL)
                                                 .Select(trade => TaxYear.ToTaxYear(trade.Date))
                                                 .Distinct();
        IEnumerable<int> taxYearsWithDividend = DividendCalculationResult.DividendSummary.Select(dividend => dividend.TaxYear).Distinct();
        return taxYearsWithDisposal.Union(taxYearsWithDividend).OrderByDescending(i => i);
    }
}
