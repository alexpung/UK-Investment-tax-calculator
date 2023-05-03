using CapitalGainCalculator.Services;
using CapitalGainCalculator.ViewModel.Options;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CapitalGainCalculator.ViewModel;

public partial class ExportToFileViewModel : ObservableObject
{
    private readonly DividendExportService _dividendExportService;
    private readonly YearOptions _yearOptions;
    private readonly UkCalculationResultExportService _calculationResultExportService;
    private readonly SaveTextFileWithDialogService _saveTextFileWithDialogService;

    public ExportToFileViewModel(DividendExportService dividendExportService, YearOptions years, UkCalculationResultExportService calculationResultExportService, SaveTextFileWithDialogService saveTextFileWithDialogService)
    {
        _dividendExportService = dividendExportService;
        _yearOptions = years;
        _calculationResultExportService = calculationResultExportService;
        _saveTextFileWithDialogService = saveTextFileWithDialogService;
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
}
