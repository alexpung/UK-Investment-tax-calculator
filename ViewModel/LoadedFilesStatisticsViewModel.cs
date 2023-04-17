using CapitalGainCalculator.Model;
using CapitalGainCalculator.ViewModel.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace CapitalGainCalculator.ViewModel;
public partial class LoadedFilesStatisticsViewModel : ObservableRecipient
{
    private readonly TaxEventLists _taxEventLists;

    [ObservableProperty]
    private int _numberOfTaxEvents = 0;
    [ObservableProperty]
    private int _numberOfTrades = 0;
    [ObservableProperty]
    private int _numberOfDividends = 0;
    [ObservableProperty]
    private int _numberOfCorporateActions = 0;

    public LoadedFilesStatisticsViewModel(TaxEventLists taxEventLists, WeakReferenceMessenger weakReferenceMessenger)
    {
        _taxEventLists = taxEventLists;
        weakReferenceMessenger.Register<DataLoadedMessage>(this, (r, m) => UpdateLoadedFilesStatistics());
    }

    public void UpdateLoadedFilesStatistics()
    {
        NumberOfTaxEvents = _taxEventLists.GetTotalNumberOfEvents();
        NumberOfDividends = _taxEventLists.Dividends.Count;
        NumberOfTrades = _taxEventLists.Trades.Count;
        NumberOfCorporateActions = _taxEventLists.CorporateActions.Count;
    }
}
