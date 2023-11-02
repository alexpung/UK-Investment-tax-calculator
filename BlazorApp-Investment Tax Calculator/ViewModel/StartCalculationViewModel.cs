using CommunityToolkit.Mvvm.Messaging;

using Model;
using Model.Interfaces;
using Model.UkTaxModel;

using ViewModel.Messages;

namespace ViewModel;

public partial class StartCalculationViewModel
{
    private readonly ITradeCalculator _tradeCalculator;
    private readonly IDividendCalculator _dividendCalculator;
    private readonly TradeCalculationResult _tradeCalculationResult;
    private readonly DividendCalculationResult _dividendCalculationResult;
    private readonly IMessenger _messenger;

    public StartCalculationViewModel(ITradeCalculator tradeCalculator, TradeCalculationResult tradeCalculationResult,
        DividendCalculationResult dividendCalculationResult, IDividendCalculator dividendCalculator, IMessenger messenger)
    {
        _tradeCalculator = tradeCalculator;
        _dividendCalculator = dividendCalculator;
        _tradeCalculationResult = tradeCalculationResult;
        _dividendCalculationResult = dividendCalculationResult;
        _messenger = messenger;
    }

    public async Task OnStartCalculation()
    {
        _tradeCalculationResult.SetResult(await Task.Run(_tradeCalculator.CalculateTax));
        _dividendCalculationResult.SetResult(await Task.Run(_dividendCalculator.CalculateTax));
        _messenger.Send<CalculationFinishedMessage>();
    }
}