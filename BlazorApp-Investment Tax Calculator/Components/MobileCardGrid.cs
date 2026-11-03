using InvestmentTaxCalculator.Model;

using Radzen;

namespace InvestmentTaxCalculator.Components;

/// <summary>
/// Supports the responsive layout of report grids. A grid with the <see cref="CssClass"/> class is shown as a
/// table when it fits its space and as one card per row when it does not (chosen by wwwroot/js/responsive-grids.js,
/// styled in app.css). Cards have no header row, so every cell carries its column title.
/// <para>Use on a grid as <c>CellRender="@(args => MobileCardGrid.LabelCell(args))"</c>.</para>
/// </summary>
public static class MobileCardGrid
{
    /// <summary>CSS class that switches a grid to the card layout on phones.</summary>
    public const string CssClass = "mobile-cards";

    public static void LabelCell<TItem>(DataGridCellRenderEventArgs<TItem> args) where TItem : notnull
    {
        var column = args.Column;
        if (column is null) return;

        string title = column.GetTitle() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(title))
        {
            args.Attributes["data-label"] = title;
        }

        // Amounts, quantities and dates must not wrap in the table layout: a negative amount would otherwise
        // break after its minus sign ("-" / "£1,234.00"). Free text is left free to wrap.
        if (IsAtomicValue(column, args.Data))
        {
            args.Attributes["data-nowrap"] = "true";
        }

        // Columns nested under a group header (e.g. "Listed Shares" / "Other Assets") repeat the same titles
        // in each group, so the first cell of each group carries the group title to head that part of the card.
        var parent = column.Parent;
        if (parent is not null && ReferenceEquals(FirstVisibleChild(parent), column))
        {
            string groupTitle = parent.GetTitle() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(groupTitle))
            {
                args.Attributes["data-group"] = groupTitle;
            }
        }
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(Type, string), Type?> PropertyTypes = new();

    // Decided from the declared type of the column's property path (e.g. "TotalInterestIncome.Amount"), not the
    // rendered text: Radzen's GetValue returns the formatted value.
    private static bool IsAtomicValue<TItem>(Radzen.Blazor.RadzenDataGridColumn<TItem> column, TItem? item) where TItem : notnull
    {
        if (item is null || string.IsNullOrEmpty(column.Property)) return false;
        Type? type = PropertyTypes.GetOrAdd((item.GetType(), column.Property), key => ResolvePropertyType(key.Item1, key.Item2));
        type = type is null ? null : Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(WrappedMoney) || type == typeof(DateTime) || type == typeof(DateOnly)
            || type == typeof(DateTimeOffset) || type == typeof(decimal) || type == typeof(double)
            || type == typeof(float) || type == typeof(int) || type == typeof(long) || type == typeof(short);
    }

    private static Type? ResolvePropertyType(Type type, string propertyPath)
    {
        Type? current = type;
        foreach (string name in propertyPath.Split('.'))
        {
            current = current?.GetProperty(name)?.PropertyType;
            if (current is null) return null;
        }
        return current;
    }

    private static object? FirstVisibleChild<TItem>(Radzen.Blazor.RadzenDataGridColumn<TItem> parent) where TItem : notnull =>
        parent.ColumnsCollection?.FirstOrDefault(child => child.GetVisible());
}
