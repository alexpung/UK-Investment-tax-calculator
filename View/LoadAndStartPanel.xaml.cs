using CapitalGainCalculator.ViewModel;
using System.Windows.Controls;

namespace CapitalGainCalculator.View;
/// <summary>
/// Interaction logic for LoadAndStartPanel.xaml
/// </summary>
public partial class LoadAndStartPanel : UserControl
{
    public LoadAndStartPanel(LoadAndStartViewModel loadAndStartViewModel)
    {
        DataContext = loadAndStartViewModel;
        InitializeComponent();
    }
}
