using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Services;
using ViewModel.Options;

namespace ViewModel;

public partial class ExportToFileViewModel : ObservableObject
{
    private readonly DividendExportService _dividendExportService;
    private readonly YearOptions _yearOptions;
    private readonly UkCalculationResultExportService _calculationResultExportService;
    private readonly SaveTextFileWithDialogService _saveTextFileWithDialogService;
    private readonly UkSection104ExportService _section104ExportService;

    public ExportToFileViewModel(DividendExportService dividendExportService, YearOptions years, UkCalculationResultExportService calculationResultExportService,
        SaveTextFileWithDialogService saveTextFileWithDialogService, UkSection104ExportService ukSection104ExportService)
    {
        _dividendExportService = dividendExportService;
        _yearOptions = years;
        _calculationResultExportService = calculationResultExportService;
        _saveTextFileWithDialogService = saveTextFileWithDialogService;
        _section104ExportService = ukSection104ExportService;
    }

    [RelayCommand]
    public void ExportDividendToFile()
    {
        string contents = _dividendExportService.Export(_yearOptions.GetSelectedYears());
        _saveTextFileWithDialogService.OpenFileDialogAndSaveText("Dividend Summary.txt", contents);
    }

    [RelayCommand]
    public void ExportTradesToFile()
    {
        string contents = _calculationResultExportService.Export(_yearOptions.GetSelectedYears());
        _saveTextFileWithDialogService.OpenFileDialogAndSaveText("Trade Calculations.txt", contents);
    }

    [RelayCommand]
    public void ExportSection104ToFile()
    {
        string contents = _section104ExportService.Export(_yearOptions.GetSelectedYears());
        _saveTextFileWithDialogService.OpenFileDialogAndSaveText("Section 104 History.txt", contents);
    }
}
