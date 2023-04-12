using Autofac;
using CapitalGainCalculator.Model;
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
        builder.RegisterType<AssetTypeToLoadSetting>().SingleInstance();
        builder.RegisterType<AssetTypeToLoadSettingViewModel>().SingleInstance();
        builder.RegisterType<SettingsPageViewModel>().SingleInstance();
        builder.RegisterType<IBParseController>().SingleInstance();
        builder.Register(c => new List<ITaxEventFileParser> { c.Resolve<IBParseController>() });
        builder.RegisterType<MainViewModel>().SingleInstance();
        return builder.Build();
    }
}
