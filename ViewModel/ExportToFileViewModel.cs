using CapitalGainCalculator.Model;
using CapitalGainCalculator.Services;
using CapitalGainCalculator.ViewModel.Options;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;

namespace CapitalGainCalculator.ViewModel;

public partial class ExportToFileViewModel : ObservableObject
{
    private readonly DividendExportService _dividendExportService;
    private readonly DividendCalculationResult _dividendCalculationResult;
    private readonly YearOptions _yearOptions;

    public ExportToFileViewModel(DividendExportService dividendExportService, DividendCalculationResult dividendCalculationResult, YearOptions years)
    {
        _dividendExportService = dividendExportService;
        _dividendCalculationResult = dividendCalculationResult;
        _yearOptions = years;
    }

    [RelayCommand]
    public void ExportDividendToFile()
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.FileName = "Dividend Summary.txt";
        saveFileDialog.DefaultExt = ".txt";
        saveFileDialog.Filter = "Text files (*.txt)|*.txt";
        bool? result = saveFileDialog.ShowDialog();
        if (result == true)
        {
            string filename = saveFileDialog.FileName;
            IEnumerable<DividendSummary> filteredDividendSummary = _dividendCalculationResult.DividendSummary.Where(dividend => _yearOptions.IsSelectedYear(dividend.TaxYear));
            System.IO.File.WriteAllText(filename, _dividendExportService.Export(filteredDividendSummary));
        }
    }
}
