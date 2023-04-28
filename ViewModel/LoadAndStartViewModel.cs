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
    private readonly TradeCalculationResult _tradeCalculationResult;
    private readonly DividendCalculationResult _dividendCalculationResult;

    public LoadAndStartViewModel(FileParseController fileParseController, TaxEventLists taxEventLists, ITradeCalculator tradeCalculator,
        TradeCalculationResult tradeCalculationResult, DividendCalculationResult dividendCalculationResult, IDividendCalculator dividendCalculator)
    {
        _fileParseController = fileParseController;
        _taxEventLists = taxEventLists;
        _tradeCalculator = tradeCalculator;
        _dividendCalculator = dividendCalculator;
        _tradeCalculationResult = tradeCalculationResult;
        _dividendCalculationResult = dividendCalculationResult;

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
        _tradeCalculationResult.SetResult(await Task.Run(_tradeCalculator.CalculateTax));
        _dividendCalculationResult.SetResult(await Task.Run(_dividendCalculator.CalculateTax));
        Messenger.Send<CalculationFinishedMessage>();
    }
}