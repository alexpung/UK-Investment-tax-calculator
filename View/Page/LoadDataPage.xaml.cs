using Wpf.Ui.Controls;

namespace CapitalGainCalculator.View.Page;
/// <summary>
/// Interaction logic for LoadDataPage.xaml
/// </summary>
public partial class LoadDataPage : UiPage
{
    public LoadDataPage(LoadAndStartPanel loadAndStartPanel, LoadedFilesStatisticsPanel loadedFilesStatisticsPanel)
    {
        InitializeComponent();
        LoadDataPageMainStackPanel.Children.Add(loadAndStartPanel);
        LoadDataPageMainStackPanel.Children.Add(loadedFilesStatisticsPanel);

    }
}
