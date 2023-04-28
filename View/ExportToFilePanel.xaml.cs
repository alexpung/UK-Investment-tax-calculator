using CapitalGainCalculator.ViewModel;
using System.Windows.Controls;

namespace CapitalGainCalculator.View;
/// <summary>
/// Interaction logic for ExportToFilePanel.xaml
/// </summary>
public partial class ExportToFilePanel : UserControl
{
    public ExportToFilePanel(ExportToFileViewModel exportToFileViewModel)
    {
        DataContext = exportToFileViewModel;
        InitializeComponent();
    }
}
