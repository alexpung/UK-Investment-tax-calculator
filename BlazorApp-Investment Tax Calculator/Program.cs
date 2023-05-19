using BlazorApp_Investment_Tax_Calculator;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Model;
using Model.Interfaces;
using Model.UkTaxModel;
using Services;
using Syncfusion.Blazor;
using ViewModel;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddSingleton<DividendExportService>();
builder.Services.AddSingleton<UkCalculationResultExportService>();
builder.Services.AddSingleton<SaveTextFileWithDialogService>();
builder.Services.AddSingleton<UkSection104ExportService>();
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
builder.Services.AddScoped<LoadAndStartViewModel>();
builder.Services.AddScoped<LoadedFilesStatisticsViewModel>();
builder.Services.AddScoped<AssetTypeToLoadSettingViewModel>();
builder.Services.AddScoped<CalculationResultSummaryViewModel>();
builder.Services.AddScoped<ExportToFileViewModel>();

builder.Services.AddSyncfusionBlazor();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
