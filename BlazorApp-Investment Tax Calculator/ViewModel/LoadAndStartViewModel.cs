using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.Components.Forms;
using Model;
using Model.Interfaces;
using Parser;
using ViewModel.Messages;

namespace ViewModel;

public partial class LoadAndStartViewModel : ObservableRecipient
{
    private readonly ITradeCalculator _tradeCalculator;
    private readonly IDividendCalculator _dividendCalculator;
    private readonly FileParseController _fileParseController;
    private readonly TaxEventLists _taxEventLists;
    private readonly TradeCalculationResult _tradeCalculationResult;
    private readonly DividendCalculationResult _dividendCalculationResult;
    private const int _maxFileCount = 100;

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

    public async Task LoadFile(IBrowserFile file)
    {
        _taxEventLists.AddData(await _fileParseController.ReadFile(file));
        Messenger.Send<DataLoadedMessage>();
    }

    public async Task OnStartCalculation()
    {
        _tradeCalculationResult.SetResult(await Task.Run(_tradeCalculator.CalculateTax));
        _dividendCalculationResult.SetResult(await Task.Run(_dividendCalculator.CalculateTax));
        Messenger.Send<CalculationFinishedMessage>();
    }
}