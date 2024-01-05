using BlazorApp_Investment_Tax_Calculator;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Model;
using Model.Interfaces;
using Model.UkTaxModel;
using Model.UkTaxModel.Futures;
using Model.UkTaxModel.Stocks;

using Parser;
using Parser.InteractiveBrokersXml;

using Services;

using Syncfusion.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddSingleton<DividendExportService>();
builder.Services.AddSingleton<FileParseController>();
builder.Services.AddSingleton<YearOptions>();

// UK tax specific components - replace if you want to calculate some other countries.
builder.Services.AddSingleton<UkCalculationResultExportService>();
builder.Services.AddSingleton<UkSection104ExportService>();
builder.Services.AddSingleton<IDividendCalculator, UkDividendCalculator>();
builder.Services.AddSingleton<ITradeCalculator, UkTradeCalculator>();
builder.Services.AddSingleton<ITradeCalculator, UkFutureTradeCalculator>();

// Register any new broker parsers here in order of priority
builder.Services.AddSingleton<ITaxEventFileParser, IBParseController>();

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

builder.Services.AddSyncfusionBlazor();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
