using CapitalGainCalculator.ViewModel;
using System.Windows.Controls;

namespace CapitalGainCalculator.View;
/// <summary>
/// Interaction logic for UserControl1.xaml
/// </summary>
public partial class LoadedFilesStatisticsPanel : UserControl
{
    public LoadedFilesStatisticsPanel(LoadedFilesStatisticsViewModel loadedFilesStatisticsViewModel)
    {
        DataContext = loadedFilesStatisticsViewModel;
        InitializeComponent();
    }
}
