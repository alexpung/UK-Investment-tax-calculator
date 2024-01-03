using Services;
using ViewModel.Options;

namespace ViewModel;

public class ExportToFileViewModel(DividendExportService dividendExportService, YearOptions years, UkCalculationResultExportService calculationResultExportService, UkSection104ExportService ukSection104ExportService)
{
    public byte[] ExportDividends()
    {
        string contents = dividendExportService.Export(years.SelectedOptions);
        return System.Text.Encoding.UTF8.GetBytes(contents);
    }

    public byte[] ExportTrades()
    {
        string contents = calculationResultExportService.PrintToTextFile(years.SelectedOptions);
        return System.Text.Encoding.UTF8.GetBytes(contents);
    }

    public byte[] ExportSection104()
    {
        string contents = ukSection104ExportService.PrintToTextFile(years.SelectedOptions);
        return System.Text.Encoding.UTF8.GetBytes(contents);
    }
}
