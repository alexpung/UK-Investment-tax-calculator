using Wpf.Ui.Controls;

namespace CapitalGainCalculator.View.Page;
/// <summary>
/// Interaction logic for LoadDataPage.xaml
/// </summary>
public partial class LoadDataPage : UiPage
{
    public LoadDataPage(LoadAndStartPanel loadAndStartPanel, LoadedFilesStatisticsPanel loadedFilesStatisticsPanel, CalculationSummaryPanel calculationSummaryPanel, ExportToFilePanel exportToFilePanel)
    {
        InitializeComponent();
        LoadDataPageMainStackPanel.Children.Add(loadAndStartPanel);
        LoadDataPageMainStackPanel.Children.Add(loadedFilesStatisticsPanel);
        LoadDataPageMainStackPanel.Children.Add(calculationSummaryPanel);
        LoadDataPageMainStackPanel.Children.Add(exportToFilePanel);
    }
}
