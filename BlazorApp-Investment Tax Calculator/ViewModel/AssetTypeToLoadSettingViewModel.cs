using CommunityToolkit.Mvvm.ComponentModel;

using Model;

namespace ViewModel;

public class AssetTypeToLoadSettingViewModel(AssetTypeToLoadSetting assetTypeToLoadSetting) : ObservableObject
{
    public AssetTypeToLoadSetting AssetTypeToLoadSetting { get; } = assetTypeToLoadSetting;

    public bool LoadStocks
    {
        get => AssetTypeToLoadSetting.LoadStocks;
        set
        {
            AssetTypeToLoadSetting.LoadStocks = value;
        }
    }

    public bool LoadDividends
    {
        get => AssetTypeToLoadSetting.LoadDividends;
        set
        {
            AssetTypeToLoadSetting.LoadDividends = value;
        }
    }

    public bool LoadOptions
    {
        get => AssetTypeToLoadSetting.LoadOptions;
        set
        {
            AssetTypeToLoadSetting.LoadOptions = value;
        }
    }

    public bool LoadFx
    {
        get => AssetTypeToLoadSetting.LoadFx;
        set
        {
            AssetTypeToLoadSetting.LoadFx = value;
        }
    }

    public bool LoadFutures
    {
        get => AssetTypeToLoadSetting.LoadFutures;
        set
        {
            AssetTypeToLoadSetting.LoadFutures = value;
        }
    }
}
