using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.Components.Forms;
using Model;
using Parser;
using ViewModel.Messages;

namespace ViewModel;

public partial class LoadFileViewModel
{
    private readonly FileParseController _fileParseController;
    private readonly TaxEventLists _taxEventLists;
    private readonly IMessenger _messenger;

    public LoadFileViewModel(FileParseController fileParseController, TaxEventLists taxEventLists, IMessenger messenger)
    {
        _fileParseController = fileParseController;
        _taxEventLists = taxEventLists;
        _messenger = messenger;
    }

    public async Task LoadFile(IBrowserFile file)
    {
        _taxEventLists.AddData(await _fileParseController.ReadFile(file));
        _messenger.Send<DataLoadedMessage>();
    }
}
