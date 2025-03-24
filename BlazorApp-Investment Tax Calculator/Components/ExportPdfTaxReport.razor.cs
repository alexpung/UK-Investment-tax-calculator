using InvestmentTaxCalculator.Services;
using InvestmentTaxCalculator.Services.PdfExport;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Syncfusion.Blazor.DropDowns;

namespace InvestmentTaxCalculator.Components;
public partial class ExportPdfTaxReport
{
    [Inject] public required IJSRuntime JSRuntime { get; set; }
    [Inject] public required PdfExportService PdfExportService { get; set; }
    [Inject] public required YearOptions YearsToExport { get; set; }
    [Inject] public required ToastService ToastService { get; set; }

    public SfListBox<string[], ISection> SectionsSelection { get; set; } = new();

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
            catch (InvalidOperationException ex)
            {
                ToastService.ShowWarning(ex.Message);
            }
            catch (Exception ex)
            {
                ToastService.ShowError(ex.Message);
                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    ToastService.ShowError(ex.StackTrace);
                }
            }
        }
    }

    private static void OnDrop(DropEventArgs<ISection> args)
    {
        if (args.Items.Count() > 1)
        {
            args.Items = [args.Items.First()];
        }
    }
    private void OnDropped(DropEventArgs<ISection> args)
    {
        if (!args.Items.Any()) return;
        ISection movedSection = args.Items.First();
        int oldIndex = PdfExportService.AllSections.IndexOf(movedSection);
        if (oldIndex != -1)
        {
            PdfExportService.AllSections.RemoveAt(oldIndex);
            PdfExportService.AllSections.Insert(args.DropIndex, movedSection);
        }
    }
}