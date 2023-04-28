using CapitalGainCalculator.ViewModel;
using System.Windows.Controls;

namespace CapitalGainCalculator.View;
/// <summary>
/// Interaction logic for CalculationSummaryPanel.xaml
/// </summary>
public partial class CalculationSummaryPanel : UserControl
{
    public CalculationSummaryPanel(CalculationResultSummaryViewModel calculationResultSummaryViewModel)
    {
        DataContext = calculationResultSummaryViewModel;
        InitializeComponent();
    }
}
