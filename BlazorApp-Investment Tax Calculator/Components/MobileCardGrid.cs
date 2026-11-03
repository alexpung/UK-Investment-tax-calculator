using Radzen;

namespace InvestmentTaxCalculator.Components;

/// <summary>
/// Supports the phone layout of report grids. On narrow screens a grid with the <see cref="CssClass"/> class
/// shows each row as a card (see app.css), so every cell needs to carry its column title: there is no visible
/// header row to read it from.
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

    private static object? FirstVisibleChild<TItem>(Radzen.Blazor.RadzenDataGridColumn<TItem> parent) where TItem : notnull =>
        parent.ColumnsCollection?.FirstOrDefault(child => child.GetVisible());
}
