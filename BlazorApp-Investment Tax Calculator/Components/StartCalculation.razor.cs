namespace InvestmentTaxCalculator.Components;
using Enumerations;

using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;
using InvestmentTaxCalculator.Services;

using Microsoft.AspNetCore.Components;

using Model;
using Model.Interfaces;
using Model.UkTaxModel;

public partial class StartCalculation
{
    [Inject] public required TaxCalculationService TaxCalculationService { get; set; }
    [Parameter] public EventCallback OnCalculated { get; set; }

    public async Task OnStartCalculation()
    {
        await TaxCalculationService.CalculateAsync();
        await OnCalculated.InvokeAsync();
    }
}
