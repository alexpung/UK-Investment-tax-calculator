using CapitalGainCalculator.ViewModel;
using Wpf.Ui.Controls;

namespace CapitalGainCalculator.View.Page;
/// <summary>
/// Interaction logic for AboutPage.xaml
/// </summary>
public partial class AboutPage : UiPage
{
    public AboutPage(AboutViewModel aboutViewModel)
    {
        DataContext = aboutViewModel;
        InitializeComponent();
    }
}
