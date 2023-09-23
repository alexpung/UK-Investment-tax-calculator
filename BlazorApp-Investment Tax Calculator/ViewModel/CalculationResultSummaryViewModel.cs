using CommunityToolkit.Mvvm.Messaging;
using Model;
using ViewModel.Messages;
using ViewModel.Options;

namespace ViewModel;

public class CalculationResultSummaryViewModel : IRecipient<CalculationFinishedMessage>, IRecipient<YearSelectionChangedMessage>
{
    private readonly TradeCalculationResult _tradeCalculationResult;
    private readonly DividendCalculationResult _dividendCalculationResult;
    public int NumberOfDisposals { get; set; }
    public WrappedMoney DisposalProceeds { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public WrappedMoney AllowableCosts { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public WrappedMoney TotalGain { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public WrappedMoney TotalLoss { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public WrappedMoney TotalDividends { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public WrappedMoney TotalForeignTaxPaid { get; set; } = WrappedMoney.GetBaseCurrencyZero();
    public YearOptions Years { get; init; }


    public CalculationResultSummaryViewModel(TradeCalculationResult tradeCalculationResult, DividendCalculationResult dividendCalculationResult, YearOptions years, IMessenger messenger)
    {
        _tradeCalculationResult = tradeCalculationResult;
        _dividendCalculationResult = dividendCalculationResult;
        Years = years;
        messenger.Register<YearSelectionChangedMessage>(this);
        messenger.Register<CalculationFinishedMessage>(this);
    }

    public void Receive(CalculationFinishedMessage message)
    {
        Years.SetYears(GetSelectableYears());
        UpdateSummary();
    }

    public void Receive(YearSelectionChangedMessage message)
    {
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        if (_tradeCalculationResult.CalculatedTrade is null || Years is null) return;
        NumberOfDisposals = _tradeCalculationResult.NumberOfDisposals(Years.SelectedOptions);
        DisposalProceeds = _tradeCalculationResult.DisposalProceeds(Years.SelectedOptions);
        AllowableCosts = _tradeCalculationResult.AllowableCosts(Years.SelectedOptions);
        TotalGain = _tradeCalculationResult.TotalGain(Years.SelectedOptions);
        TotalLoss = _tradeCalculationResult.TotalLoss(Years.SelectedOptions);
        TotalDividends = _dividendCalculationResult.GetTotalDividend(Years.SelectedOptions);
        TotalForeignTaxPaid = _dividendCalculationResult.GetForeignTaxPaid(Years.SelectedOptions);
    }

    private IEnumerable<int> GetSelectableYears()
    {
        IEnumerable<int> yearsWithDisposal = _tradeCalculationResult.CalculatedTrade.Where(trade => trade.BuySell == Enum.TradeType.SELL)
                                                 .Select(trade => trade.Date.Year)
                                                 .Distinct();
        IEnumerable<int> yearsWithDividend = _dividendCalculationResult.DividendSummary.Select(dividend => dividend.TaxYear).Distinct();
        return yearsWithDisposal.Union(yearsWithDividend).OrderByDescending(i => i);
    }
}
