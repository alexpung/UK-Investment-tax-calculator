using Autofac;
using CapitalGainCalculator.ViewModel;
using System.ComponentModel;
using System.Windows.Controls;

namespace CapitalGainCalculator.View;
/// <summary>
/// Interaction logic for LoadAndStartPanel.xaml
/// </summary>
public partial class LoadAndStartPanel : UserControl
{
    public LoadAndStartPanel()
    {
        InitializeComponent();
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            DataContext = App.Current.IocContainer.Resolve<LoadAndStartViewModel>();
        }
    }
}
