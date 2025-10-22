using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.TaxEvents;

using Microsoft.AspNetCore.Components;

namespace InvestmentTaxCalculator.Components;

public partial class InterestIncomeGrid
{
    private static RenderFragment<object> AmountTemplate => context =>
    {
        var income = context as InterestIncome;
        return builder =>
        {
            if (income == null) return;
            builder.AddContent(0, income.Amount?.Display() ?? string.Empty);
        };
    };
    private static RenderFragment<object> TypeTemplate => context =>
    {
        var income = context as InterestIncome;
        return builder =>
        {
            if (income == null) return;
            builder.AddContent(0, income.InterestType.GetDescription());
        };
    };

    private RenderFragment<object> CheckboxTemplate => context =>
    {
        return builder =>
        {
            if (context is not InterestIncome income) return;
            // Only show checkbox for accrued income profit or loss
            if (income.InterestType is not (InterestType.ACCURREDINCOMEPROFIT or InterestType.ACCURREDINCOMELOSS))
            {
                builder.AddContent(0, string.Empty);
                return;
            }
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "type", "checkbox");
            builder.AddAttribute(2, "checked", income.IsNextPaymentInSameTaxYear);
            builder.AddAttribute(3, "onchange", EventCallback.Factory.Create(this, async (ChangeEventArgs e) =>
            {
                income.IsNextPaymentInSameTaxYear = (bool)e.Value!;
                await InvokeAsync(StateHasChanged);
            }));
            builder.CloseElement();
        };
    };
}
