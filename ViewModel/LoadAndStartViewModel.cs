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

public partial class LoadAndStartViewModel : ObservableRecipient
{
    private readonly ITradeCalculator _tradeCalculator;
    private readonly IDividendCalculator _dividendCalculator;
    private readonly FileParseController _fileParseController;
    private readonly TaxEventLists _taxEventLists;
    private readonly CalculationResult _calculationResult;

    public LoadAndStartViewModel(FileParseController fileParseController, TaxEventLists taxEventLists, ITradeCalculator tradeCalculator,
        CalculationResult calculationResult, IDividendCalculator dividendCalculator)
    {
        _fileParseController = fileParseController;
        _taxEventLists = taxEventLists;
        _tradeCalculator = tradeCalculator;
        _dividendCalculator = dividendCalculator;
        _calculationResult = calculationResult;

        IsActive = true;
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
            Messenger.Send<DataLoadedMessage>();
        }
    }

    [RelayCommand]
    public async Task OnReadFiles()
    {
    }

    [RelayCommand]
    public async Task OnStartCalculation()
    {
        _calculationResult.SetResult(await Task.Run(_tradeCalculator.CalculateTax));
        _calculationResult.SetResult(await Task.Run(_dividendCalculator.CalculateTax));
        Messenger.Send<CalculationFinishedMessage>();
    }
}