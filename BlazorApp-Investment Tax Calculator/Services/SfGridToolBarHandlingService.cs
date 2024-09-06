using Syncfusion.Blazor.Grids;

namespace InvestmentTaxCalculator.Services;

public class SfGridToolBarHandlingService
{
    public async Task ToolbarClickHandler<T>(Syncfusion.Blazor.Navigations.ClickEventArgs args, SfGrid<T> table)
    {
        if (args.Item.Id.EndsWith("pdfexport"))
        {
            var pdfExportProperties = new PdfExportProperties()
            {
                PageOrientation = PageOrientation.Landscape,
                PageSize = PdfPageSize.A4,
                IncludeTemplateColumn = true
            };
            await table.ExportToPdfAsync(pdfExportProperties);
        }
        if (args.Item.Id.EndsWith("excelexport"))
        {
            ExcelExportProperties exportProperties = new()
            {
                IncludeTemplateColumn = true
            };
            await table.ExportToExcelAsync(exportProperties);
        }
    }
}
