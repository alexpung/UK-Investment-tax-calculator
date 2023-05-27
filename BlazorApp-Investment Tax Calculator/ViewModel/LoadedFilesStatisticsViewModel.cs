using CommunityToolkit.Mvvm.Messaging;
using Model;
using ViewModel.Messages;

namespace ViewModel;
public partial class LoadedFilesStatisticsViewModel : IRecipient<DataLoadedMessage>
{
    private readonly TaxEventLists _taxEventLists;

    public int NumberOfTaxEvents { get; set; } = 0;
    public int NumberOfTrades { get; set; } = 0;
    public int NumberOfDividends { get; set; } = 0;
    public int NumberOfCorporateActions { get; set; } = 0;

    public LoadedFilesStatisticsViewModel(TaxEventLists taxEventLists, IMessenger messenger)
    {
        _taxEventLists = taxEventLists;
        messenger.Register(this);
    }

    public void Receive(DataLoadedMessage dataLoadedMessage)
    {
        NumberOfTaxEvents = _taxEventLists.GetTotalNumberOfEvents();
        NumberOfDividends = _taxEventLists.Dividends.Count;
        NumberOfTrades = _taxEventLists.Trades.Count;
        NumberOfCorporateActions = _taxEventLists.CorporateActions.Count;
    }
}
