namespace InvestmentTaxCalculator.Components;

using InvestmentTaxCalculator.Services;

using Microsoft.AspNetCore.Components;

public partial class StartCalculation : IDisposable
{
    [Inject] public required TaxCalculationService TaxCalculationService { get; set; }
    [Inject] public required FileImportStateService FileImportState { get; set; }
    [Parameter] public EventCallback OnCalculated { get; set; }

    protected override void OnInitialized()
    {
        FileImportState.OnChange += StateHasChanged;
    }

    public void Dispose()
    {
        FileImportState.OnChange -= StateHasChanged;
    }

    public async Task OnStartCalculation()
    {
        await TaxCalculationService.CalculateAsync();
        await OnCalculated.InvokeAsync();
    }
}
