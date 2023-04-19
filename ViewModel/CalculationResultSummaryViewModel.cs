using CapitalGainCalculator.Model;
using CapitalGainCalculator.Model.Interfaces;
using CapitalGainCalculator.ViewModel.Messages;
using CapitalGainCalculator.ViewModel.Options;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;
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
    public YearOptions Years { get; init; }
    private readonly ITaxYear _taxYear;


    public CalculationResultSummaryViewModel(CalculationResult calculationResult, YearOptions years, ITaxYear taxYear)
    {
        _calculationResult = calculationResult;
        Years = years;
        Years.PropertyChanged += Years_PropertyChanged;
        _taxYear = taxYear;
        IsActive = true;
    }

    private void Years_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (Years.SelectedYear is not null)
        {
            UpdateSummary();
        }
    }

    public void Receive(CalculationFinishedMessage message)
    {
        UpdateSummary();
        Years.SetYears(GetYearsWithDisposal());
    }

    private void UpdateSummary()
    {
        if (_calculationResult.CalculatedTrade is null || Years is null) return;
        IEnumerable<TradeTaxCalculation> resultFilterByYear = _calculationResult.CalculatedTrade.Where(trade => Years.IsSelectedYear(_taxYear.ToTaxYear(trade.Date)));
        NumberOfDisposals = resultFilterByYear.Count(trade => trade.BuySell == Enum.TradeType.SELL);
        DisposalProceeds = resultFilterByYear.Sum(trade => trade.TotalProceeds);
        AllowableCosts = resultFilterByYear.Sum(trade => trade.TotalAllowableCost);
        TotalGain = resultFilterByYear.Where(trade => trade.Gain > 0).Sum(trade => trade.Gain);
        TotalLoss = resultFilterByYear.Where(trade => trade.Gain < 0).Sum(trade => trade.Gain);
    }

    private IEnumerable<int> GetYearsWithDisposal()
    {
        return _calculationResult.CalculatedTrade.Where(trade => trade.BuySell == Enum.TradeType.SELL)
                                                 .Select(trade => trade.Date.Year)
                                                 .Distinct()
                                                 .OrderByDescending(i => i);
    }
}
