using CapitalGainCalculator.Model;
using CapitalGainCalculator.Model.Interfaces;
using CapitalGainCalculator.Model.UkTaxModel;
using CapitalGainCalculator.Parser;
using CapitalGainCalculator.Parser.InteractiveBrokersXml;
using CapitalGainCalculator.Service;
using CapitalGainCalculator.View;
using CapitalGainCalculator.View.Page;
using CapitalGainCalculator.ViewModel;
using CapitalGainCalculator.ViewModel.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using Wpf.Ui.Mvvm.Contracts;
using Wpf.Ui.Mvvm.Services;

namespace CapitalGainCalculator;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private static readonly IHost _host = Host
        .CreateDefaultBuilder()
        .ConfigureServices((context, services) =>
        {
            // App Host
            services.AddHostedService<ApplicationHostService>();

            // Service containing navigation, same as INavigationWindow... but without window
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IPageService, PageService>();

            //Models
            services.AddSingleton<TaxEventLists>();
            services.AddSingleton<ITradeCalculator, UkTradeCalculator>();
            services.AddSingleton<AssetTypeToLoadSetting>();

            // Main window with navigation
            services.AddScoped<MainWindow>();
            services.AddScoped<MainViewModel>();

            // Views and ViewModels
            services.AddScoped<LoadDataPage>();
            services.AddScoped<SettingsPage>();
            services.AddScoped<LoadAndStartPanel>();
            services.AddScoped<LoadAndStartViewModel>();
            services.AddScoped<LoadedFilesStatisticsPanel>();
            services.AddScoped<LoadedFilesStatisticsViewModel>();
            services.AddScoped<AssetTypeLoadOptionsPanel>();
            services.AddScoped<AssetTypeToLoadSettingViewModel>();
            services.AddScoped<CalculationSummaryPanel>();
            services.AddScoped<CalculationResultSummaryViewModel>();

            //Application logic
            services.AddSingleton<IBParseController>();
            //Represent a list of ITaxEventFileParser in desending order of priority. A file that is accepted by two or more ITaxEventFileParser will be taken by the earlier one in the IEnumerable
            services.AddSingleton<IEnumerable<ITaxEventFileParser>>(c => new List<ITaxEventFileParser> { c.GetService<IBParseController>()! });
            services.AddSingleton<FileParseController>();
            services.AddSingleton<TaxEventLists>();
            services.AddSingleton<UkSection104Pools>();
            services.AddSingleton<CalculationResult>();
            services.AddScoped<YearOptions>();
            services.AddSingleton<ITaxYear, UKTaxYear>();
        }).Build();

    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Occurs when the application is loading.
    /// </summary>
    private async void OnStartup(object sender, StartupEventArgs e)
    {
        await _host.StartAsync();
    }

    /// <summary>
    /// Occurs when the application is closing.
    /// </summary>
    private async void OnExit(object sender, ExitEventArgs e)
    {
        await _host.StopAsync();

        _host.Dispose();
    }

    /// <summary>
    /// Occurs when an exception is thrown by an application but not handled.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
    }
}
