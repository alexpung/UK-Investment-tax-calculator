using CommunityToolkit.Mvvm.Messaging;

using Microsoft.AspNetCore.Components.Forms;

using Model;

using Parser;

using ViewModel.Messages;

namespace ViewModel;

public partial class LoadFileViewModel(FileParseController fileParseController, TaxEventLists taxEventLists, IMessenger messenger)
{
    private readonly TaxEventLists _taxEventLists = taxEventLists;

    public async Task LoadFile(IBrowserFile file)
    {
        _taxEventLists.AddData(await fileParseController.ReadFile(file));
        messenger.Send<DataLoadedMessage>();
    }
}
