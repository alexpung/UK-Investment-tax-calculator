using Wpf.Ui.Controls;

namespace CapitalGainCalculator.View.Page
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : UiPage
    {
        public SettingsPage(AssetTypeLoadOptionsPanel assetTypeLoadOptionsPanel)
        {
            InitializeComponent();
            MainStackPanel.Children.Add(assetTypeLoadOptionsPanel);
        }
    }
}
