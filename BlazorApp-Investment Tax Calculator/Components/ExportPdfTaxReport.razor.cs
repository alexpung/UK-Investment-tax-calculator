using InvestmentTaxCalculator.Services;
using InvestmentTaxCalculator.Services.PdfExport;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace InvestmentTaxCalculator.Components;
public partial class ExportPdfTaxReport
{
    [Inject] public required IJSRuntime JSRuntime { get; set; }
    [Inject] public required PdfExportService PdfExportService { get; set; }
    [Inject] public required YearOptions YearsToExport { get; set; }
    [Inject] public required ToastService ToastService { get; set; }

    private IJSObjectReference? _downloadFileJsScript;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        _downloadFileJsScript = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/ExportFile.razor.js");
    }

    private async Task InvokeExportToPdf()
    {
        foreach (int year in YearsToExport.SelectedOptions)
        {
            try
            {
                using var streamRef = new DotNetStreamReference(PdfExportService.CreatePdf(year));
                await _downloadFileJsScript!.InvokeVoidAsync("BlazorDownloadFile", $"Tax Report {year}.pdf", streamRef);
            }
            catch (Exception ex)
            {
                ToastService.ShowError(ex.Message);
            }
        }
    }
}