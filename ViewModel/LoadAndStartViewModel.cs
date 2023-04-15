using CapitalGainCalculator.Model;
using CapitalGainCalculator.Model.Interfaces;
using CapitalGainCalculator.Parser;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CapitalGainCalculator.ViewModel;

public class LoadAndStartViewModel : ObservableObject
{
    private readonly ICalculator _calculator;
    private readonly FileParseController _fileParseController;
    private List<TradeTaxCalculation> _results = new();
    public AsyncRelayCommand ReadFolderCommand { get; set; }
    public AsyncRelayCommand ReadFilesCommand { get; set; }
    public AsyncRelayCommand StartCalculationCommand { get; set; }

    public LoadAndStartViewModel(ICalculator calculator, FileParseController fileParseController)
    {
        _fileParseController = fileParseController;
        _calculator = calculator;
        ReadFilesCommand = new(OnReadFiles);
        ReadFolderCommand = new(OnReadFolder);
        StartCalculationCommand = new(OnStartCalculation);
    }

    public async Task OnReadFolder()
    {
        FolderBrowserDialog openFileDlg = new();
        var result = openFileDlg.ShowDialog();
        if (result == DialogResult.OK)
        {
            string path = openFileDlg.SelectedPath;
            _calculator.AddTaxEvents(_fileParseController.ParseFolder(path));
        }
    }

    public async Task OnReadFiles()
    {
    }

    public async Task OnStartCalculation()
    {
        List<TradeTaxCalculation> results = await Task.Run(_calculator.CalculateTax);
        _results = results;
    }
}