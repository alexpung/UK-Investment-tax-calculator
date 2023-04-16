using Wpf.Ui.Controls;

namespace CapitalGainCalculator.View.Page;
/// <summary>
/// Interaction logic for LoadDataPage.xaml
/// </summary>
public partial class LoadDataPage : UiPage
{
    public LoadDataPage(LoadAndStartPanel loadAndStartPanel)
    {
        InitializeComponent();
        LoadDataPageMainStackPanel.Children.Add(loadAndStartPanel);

    }
}
