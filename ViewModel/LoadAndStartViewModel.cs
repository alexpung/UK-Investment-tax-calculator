using CapitalGainCalculator.Model;
using CapitalGainCalculator.Model.Interfaces;
using CapitalGainCalculator.Parser;
using CapitalGainCalculator.ViewModel.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CapitalGainCalculator.ViewModel;

public partial class LoadAndStartViewModel : ObservableObject
{
    private readonly ICalculator _calculator;
    private readonly FileParseController _fileParseController;
    private readonly TaxEventLists _taxEventLists;
    private readonly CalculationResult _calculationResult;
    private readonly WeakReferenceMessenger _messenger;

    public LoadAndStartViewModel(FileParseController fileParseController, TaxEventLists taxEventLists, ICalculator calculator, CalculationResult calculationResult, WeakReferenceMessenger weakReferenceMessenger)
    {
        _fileParseController = fileParseController;
        _taxEventLists = taxEventLists;
        _calculator = calculator;
        _calculationResult = calculationResult;
        _messenger = weakReferenceMessenger;
    }

    [RelayCommand]
    public async Task OnReadFolder()
    {
        FolderBrowserDialog openFileDlg = new();
        var result = openFileDlg.ShowDialog();
        if (result == DialogResult.OK)
        {
            string path = openFileDlg.SelectedPath;
            _taxEventLists.AddData(_fileParseController.ParseFolder(path));
            _messenger.Send<DataLoadedMessage>();
        }
    }

    [RelayCommand]
    public async Task OnReadFiles()
    {
    }

    [RelayCommand]
    public async Task OnStartCalculation()
    {
        _calculationResult.SetResult(await Task.Run(_calculator.CalculateTax));
    }
}