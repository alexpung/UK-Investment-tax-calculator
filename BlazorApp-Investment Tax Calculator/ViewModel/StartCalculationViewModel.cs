using CommunityToolkit.Mvvm.Messaging;

using Model;
using Model.Interfaces;
using Model.UkTaxModel;

using ViewModel.Messages;

namespace ViewModel;

public partial class StartCalculationViewModel(IEnumerable<ITradeCalculator> tradeCalculators, TradeCalculationResult tradeCalculationResult,
    DividendCalculationResult dividendCalculationResult, IDividendCalculator dividendCalculator, IMessenger messenger, UkSection104Pools section104Pools)
{
    public async Task OnStartCalculation()
    {
        section104Pools.Clear();
        tradeCalculationResult.Clear();
        foreach (ITradeCalculator tradeCalculator in tradeCalculators)
        {
            tradeCalculationResult.SetResult(await Task.Run(tradeCalculator.CalculateTax));
        }
        dividendCalculationResult.SetResult(await Task.Run(dividendCalculator.CalculateTax));
        messenger.Send<CalculationFinishedMessage>();
    }
}