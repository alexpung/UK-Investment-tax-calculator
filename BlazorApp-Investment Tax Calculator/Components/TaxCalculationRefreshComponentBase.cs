using InvestmentTaxCalculator.Services;

using Microsoft.AspNetCore.Components;
using Radzen.Blazor;

namespace InvestmentTaxCalculator.Components;

public abstract class TaxCalculationRefreshComponentBase : ComponentBase, IDisposable
{
    [Inject] protected TaxCalculationService TaxCalculationService { get; set; } = default!;

    protected override void OnInitialized()
    {
        RefreshData();
        TaxCalculationService.OnStateChanged += HandleTaxCalculationStateChanged;
    }

    protected abstract void RefreshData();

    protected virtual Task RefreshRenderedGridsAsync() => Task.CompletedTask;

    protected static Task RefreshGridAsync<TGridItem>(RadzenDataGrid<TGridItem>? grid) where TGridItem : notnull
    {
        return grid is null ? Task.CompletedTask : grid.Reload();
    }

    private void HandleTaxCalculationStateChanged()
    {
        _ = InvokeAsync(async () =>
        {
            if (TaxCalculationService.IsCalculating)
            {
                return;
            }

            RefreshData();
            StateHasChanged();

            await RefreshRenderedGridsAsync();
        });
    }

    public virtual void Dispose()
    {
        TaxCalculationService.OnStateChanged -= HandleTaxCalculationStateChanged;
    }
}
