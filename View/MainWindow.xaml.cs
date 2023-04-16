using CapitalGainCalculator.ViewModel;
using System;
using Wpf.Ui.Controls;
using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Mvvm.Contracts;

namespace CapitalGainCalculator.View;
/// <summary>
/// Interaction logic for Window1.xaml
/// </summary>
public partial class MainWindow : UiWindow
{
    public MainViewModel ViewModel { get; }
    public MainWindow(MainViewModel mainViewModel, IPageService pageService, INavigationService navigationService)
    {
        ViewModel = mainViewModel;
        DataContext = this;
        InitializeComponent();
        SetPageService(pageService);
        navigationService.SetNavigationControl(RootNavigation);
    }

    #region INavigationWindow methods

    public INavigation GetNavigation()
        => RootNavigation;

    public bool Navigate(Type pageType)
        => RootNavigation.Navigate(pageType);

    public void SetPageService(IPageService pageService)
        => RootNavigation.PageService = pageService;

    public void ShowWindow()
        => Show();

    public void CloseWindow()
        => Close();

    #endregion INavigationWindow methods
}
