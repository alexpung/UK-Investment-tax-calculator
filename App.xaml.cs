using Autofac;
using CapitalGainCalculator.Model;
using CapitalGainCalculator.Model.Interfaces;
using CapitalGainCalculator.Model.UkTaxModel;
using CapitalGainCalculator.Parser;
using CapitalGainCalculator.Parser.InteractiveBrokersXml;
using CapitalGainCalculator.ViewModel;
using System.Collections.Generic;
using System.Windows;

namespace CapitalGainCalculator;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public sealed partial class App : Application
{
    public IContainer IocContainer { get; }
    public App()
    {
        IocContainer = ConfigureServices();
        InitializeComponent();
    }
    public new static App Current => (App)Application.Current;

    /// <summary>
    /// Configures the services for the application.
    /// </summary>
    private static IContainer ConfigureServices()
    {
        ContainerBuilder builder = new();
        builder.RegisterType<TaxEventLists>().SingleInstance();
        builder.RegisterType<UkTradeCalculator>().As<ICalculator>();
        builder.RegisterType<AssetTypeToLoadSetting>().SingleInstance();

        builder.RegisterType<AssetTypeToLoadSettingViewModel>().SingleInstance();
        builder.RegisterType<SettingsPageViewModel>().SingleInstance();
        builder.RegisterType<LoadAndStartViewModel>().SingleInstance();
        builder.RegisterType<MainViewModel>().SingleInstance();

        builder.RegisterType<IBParseController>().SingleInstance();
        builder.Register(c => new List<ITaxEventFileParser> { c.Resolve<IBParseController>() }).As<IEnumerable<ITaxEventFileParser>>();
        builder.RegisterType<FileParseController>().SingleInstance();

        return builder.Build();
    }
}
