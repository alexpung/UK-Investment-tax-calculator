using BlazorApp_Investment_Tax_Calculator;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Model;
using Model.Interfaces;
using Model.UkTaxModel;
using Parser;
using Parser.InteractiveBrokersXml;
using Services;
using Syncfusion.Blazor;
using ViewModel;
using ViewModel.Options;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddSingleton<IMessenger>(new WeakReferenceMessenger());
builder.Services.AddSingleton<DividendExportService>();
builder.Services.AddSingleton<UkCalculationResultExportService>();
builder.Services.AddSingleton<SaveTextFileWithDialogService>();
builder.Services.AddSingleton<UkSection104ExportService>();
builder.Services.AddSingleton<FileParseController>();
builder.Services.AddSingleton<ITradeCalculator, UkTradeCalculator>();
builder.Services.AddSingleton<IDividendCalculator, UkDividendCalculator>();
builder.Services.AddSingleton<IBParseController>();
builder.Services.AddSingleton<YearOptions>();
// Register any new broker parsers here in order of priority
builder.Services.AddSingleton<IEnumerable<ITaxEventFileParser>>(c => new List<ITaxEventFileParser> { c.GetService<IBParseController>()! });
// Models
TaxEventLists taxEventLists = new();
builder.Services.AddSingleton<IDividendLists>(taxEventLists);
builder.Services.AddSingleton<ITradeAndCorporateActionList>(taxEventLists);
builder.Services.AddSingleton(taxEventLists);
builder.Services.AddSingleton<AssetTypeToLoadSetting>();
builder.Services.AddSingleton<UkSection104Pools>();
builder.Services.AddSingleton<TradeCalculationResult>();
builder.Services.AddSingleton<DividendCalculationResult>();
builder.Services.AddSingleton<ITaxYear, UKTaxYear>();
// View Models
builder.Services.AddScoped<StartCalculationViewModel>();
builder.Services.AddScoped<LoadFileViewModel>();
builder.Services.AddScoped<LoadedFilesStatisticsViewModel>();
builder.Services.AddScoped<AssetTypeToLoadSettingViewModel>();
builder.Services.AddScoped<CalculationResultSummaryViewModel>();
builder.Services.AddScoped<ExportToFileViewModel>();

builder.Services.AddSyncfusionBlazor();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
