using CommunityToolkit.Mvvm.Messaging;

using Model;
using Model.Interfaces;
using Model.UkTaxModel;

using ViewModel.Messages;

namespace ViewModel;

public partial class StartCalculationViewModel
{
    private readonly IEnumerable<ITradeCalculator> _tradeCalculators;
    private readonly IDividendCalculator _dividendCalculator;
    private readonly TradeCalculationResult _tradeCalculationResult;
    private readonly DividendCalculationResult _dividendCalculationResult;
    private readonly IMessenger _messenger;
    private readonly UkSection104Pools _ukSection104Pools;

    public StartCalculationViewModel(IEnumerable<ITradeCalculator> tradeCalculators, TradeCalculationResult tradeCalculationResult,
        DividendCalculationResult dividendCalculationResult, IDividendCalculator dividendCalculator, IMessenger messenger, UkSection104Pools section104Pools)
    {
        _tradeCalculators = tradeCalculators;
        _dividendCalculator = dividendCalculator;
        _tradeCalculationResult = tradeCalculationResult;
        _dividendCalculationResult = dividendCalculationResult;
        _messenger = messenger;
        _ukSection104Pools = section104Pools;
    }

    public async Task OnStartCalculation()
    {
        _ukSection104Pools.Clear();
        _tradeCalculationResult.Clear();
        foreach (ITradeCalculator tradeCalculator in _tradeCalculators)
        {
            _tradeCalculationResult.SetResult(await Task.Run(tradeCalculator.CalculateTax));
        }
        _dividendCalculationResult.SetResult(await Task.Run(_dividendCalculator.CalculateTax));
        _messenger.Send<CalculationFinishedMessage>();
    }
}