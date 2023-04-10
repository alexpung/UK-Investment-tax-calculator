using CommunityToolkit.Mvvm.ComponentModel;

namespace CapitalGainCalculator.Model;
public partial class AssetTypeToLoadSetting : ObservableObject
{
    [ObservableProperty]
    private bool _loadStocks;
    [ObservableProperty]
    private bool _loadOptions;
    [ObservableProperty]
    private bool _loadFutures;
    [ObservableProperty]
    private bool _loadFx;
    [ObservableProperty]
    private bool _loadDividend;
}
