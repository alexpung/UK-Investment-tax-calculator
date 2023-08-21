using Services;
using ViewModel.Options;

namespace ViewModel;

public class ExportToFileViewModel
{
    private readonly DividendExportService _dividendExportService;
    private readonly YearOptions _yearOptions;
    private readonly UkCalculationResultExportService _calculationResultExportService;
    private readonly UkSection104ExportService _section104ExportService;

    public ExportToFileViewModel(DividendExportService dividendExportService, YearOptions years, UkCalculationResultExportService calculationResultExportService, UkSection104ExportService ukSection104ExportService)
    {
        _dividendExportService = dividendExportService;
        _yearOptions = years;
        _calculationResultExportService = calculationResultExportService;
        _section104ExportService = ukSection104ExportService;
    }

    public byte[] ExportDividends()
    {
        string contents = _dividendExportService.Export(_yearOptions.SelectedOptions);
        return System.Text.Encoding.UTF8.GetBytes(contents);
    }

    public byte[] ExportTrades()
    {
        string contents = _calculationResultExportService.PrintToTextFile(_yearOptions.SelectedOptions);
        return System.Text.Encoding.UTF8.GetBytes(contents);
    }

    public byte[] ExportSection104()
    {
        string contents = _section104ExportService.PrintToTextFile(_yearOptions.SelectedOptions);
        return System.Text.Encoding.UTF8.GetBytes(contents);
    }
}
