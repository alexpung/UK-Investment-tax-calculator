using CapitalGainCalculator.View.Page;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using Wpf.Ui.Controls.Interfaces;

namespace CapitalGainCalculator.ViewModel;

public partial class MainViewModel : ObservableObject
{
    private bool _isInitialized = false;

    [ObservableProperty]
    private string _applicationTitle = string.Empty;

    [ObservableProperty]
    private ObservableCollection<INavigationControl> _navigationItems = new();

    [ObservableProperty]
    private ObservableCollection<INavigationControl> _navigationFooter = new();

    [ObservableProperty]
    private ObservableCollection<INavigationControl> _trayMenuItems = new();

    public MainViewModel()
    {
        if (!_isInitialized)
            InitializeViewModel();
    }

    private void InitializeViewModel()
    {
        ApplicationTitle = "UK Investment Tax Calculator";

        NavigationItems = new ObservableCollection<INavigationControl>
            {
                new NavigationItem()
                {
                    Content = "Load Data",
                    Icon = SymbolRegular.Calculator24,
                    RenderSize = new Size(80, 80),
                    PageType = typeof(LoadDataPage)
                },
                new NavigationItem()
                {
                    Content = "Dividends",
                    RenderSize = new Size(80, 80),
                    Icon = SymbolRegular.Money24
                },
                new NavigationItem()
                {
                    Content = "Capital Gains",
                    RenderSize = new Size(300, 300),
                    Icon = SymbolRegular.ChartMultiple24,
                    FontSize = 10

                }
            };

        NavigationFooter = new ObservableCollection<INavigationControl>
            {
                new NavigationItem()
                {
                    Content = "Settings",
                    Icon = SymbolRegular.Settings24,
                    PageType = typeof(SettingsPage)
                }
            };

        _isInitialized = true;
    }
}