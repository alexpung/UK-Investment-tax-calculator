using CapitalGainCalculator.ViewModel;
using System.Windows.Controls;

namespace CapitalGainCalculator.View;
/// <summary>
/// Interaction logic for UserControl1.xaml
/// </summary>
public partial class AssetTypeLoadOptionsPanel : UserControl
{
    public AssetTypeLoadOptionsPanel(AssetTypeToLoadSettingViewModel assetTypeToLoadSettingViewModel)
    {
        DataContext = assetTypeToLoadSettingViewModel;
        InitializeComponent();
        LoadDividendsCheckBox.IsEnabled = true;
        LoadStocksCheckBox.IsEnabled = true;
        LoadFuturesCheckBox.IsEnabled = false;
        LoadOptionsCheckBox.IsEnabled = false;
        LoadFxCheckBox.IsEnabled = false;

        LoadDividendsCheckBox.IsChecked = true;
        LoadStocksCheckBox.IsChecked = true;

    }
}
