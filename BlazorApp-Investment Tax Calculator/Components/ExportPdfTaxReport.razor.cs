using InvestmentTaxCalculator.Services.PdfExport;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PdfSharp.Fonts;

namespace InvestmentTaxCalculator.Components;
public partial class ExportPdfTaxReport
{
    [Inject] public required IJSRuntime JSRuntime { get; set; }
    [Inject] public required CustomFontResolver CustomFontResolver { get; set; }
    [Inject] public required PdfExportService PdfExportService { get; set; }

    private IJSObjectReference? _downloadFileJsScript;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        _downloadFileJsScript = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/ExportFile.razor.js");
        if (firstRender)
        {
            await CustomFontResolver.InitializeFontsAsync();
            GlobalFontSettings.FontResolver = CustomFontResolver;
        }
    }

    private async Task InvokeExportToPdf()
    {
        using var streamRef = new DotNetStreamReference(PdfExportService.CreatePdf());
        await _downloadFileJsScript!.InvokeVoidAsync("BlazorDownloadFile", "Tax Report.pdf", streamRef);
    }
}