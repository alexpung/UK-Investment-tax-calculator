namespace InvestmentTaxCalculator.Components;

using InvestmentTaxCalculator.Services;

using Microsoft.AspNetCore.Components;

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
