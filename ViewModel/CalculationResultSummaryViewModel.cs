using CapitalGainCalculator.Model;
using CapitalGainCalculator.ViewModel.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Linq;

namespace CapitalGainCalculator.ViewModel;

public partial class CalculationResultSummaryViewModel : ObservableRecipient, IRecipient<CalculationFinishedMessage>
{
    private readonly CalculationResult _calculationResult;
    [ObservableProperty]
    private int _numberOfDisposals;
    [ObservableProperty]
    private decimal _disposalProceeds;
    [ObservableProperty]
    private decimal _allowableCosts;
    [ObservableProperty]
    private decimal _totalGain;
    [ObservableProperty]
    private decimal _totalLoss;

    public CalculationResultSummaryViewModel(CalculationResult calculationResult)
    {
        _calculationResult = calculationResult;
        IsActive = true;
    }

    public void Receive(CalculationFinishedMessage message)
    {
        NumberOfDisposals = _calculationResult.CalculatedTrade.Count(trade => trade.BuySell == Enum.TradeType.SELL);
        DisposalProceeds = _calculationResult.CalculatedTrade.Sum(trade => trade.TotalProceeds);
        AllowableCosts = _calculationResult.CalculatedTrade.Sum(trade => trade.TotalAllowableCost);
        TotalGain = _calculationResult.CalculatedTrade.Where(trade => trade.Gain > 0).Sum(trade => trade.Gain);
        TotalLoss = _calculationResult.CalculatedTrade.Where(trade => trade.Gain < 0).Sum(trade => trade.Gain);
    }
}
