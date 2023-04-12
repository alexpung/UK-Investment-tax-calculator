using Autofac;
using CapitalGainCalculator.ViewModel;
using System.ComponentModel;
using System.Windows.Controls;

namespace CapitalGainCalculator.View;
/// <summary>
/// Interaction logic for UserControl1.xaml
/// </summary>
public partial class AssetTypeLoadOptionsPanel : UserControl
{
    public AssetTypeLoadOptionsPanel()
    {
        InitializeComponent();
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            DataContext = App.Current.IocContainer.Resolve<AssetTypeToLoadSettingViewModel>();
        }
    }
}
