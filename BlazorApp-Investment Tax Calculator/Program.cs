using InvestmentTaxCalculator;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Futures;
using InvestmentTaxCalculator.Model.UkTaxModel.Options;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;
using InvestmentTaxCalculator.Parser;
using InvestmentTaxCalculator.Parser.InteractiveBrokersXml;
using InvestmentTaxCalculator.Services;
using InvestmentTaxCalculator.Services.PdfExport;
using InvestmentTaxCalculator.ViewModel;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Parser.InteractiveBrokersXml;

using PdfSharp.Fonts;

using Syncfusion.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
// app services
builder.Services.AddSingleton<DividendExportService>();
builder.Services.AddSingleton<FileParseController>();
builder.Services.AddSingleton<YearOptions>();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddSingleton<SfGridToolBarHandlingService>();
builder.Services.AddSingleton<ExportTaxEventService>();
builder.Services.AddSingleton<TaxYearReportService>();
builder.Services.AddSingleton<TaxYearCgtByTypeReportService>();
builder.Services.AddSingleton<PdfExportService>();

// UK tax specific components - replace if you want to calculate some other countries.
builder.Services.AddSingleton<UkCalculationResultExportService>();
builder.Services.AddSingleton<UkSection104ExportService>();
// Order is important. Option trades need to calculate before its derivatives.
builder.Services.AddSingleton<IDividendCalculator, UkDividendCalculator>();
builder.Services.AddSingleton<ITradeCalculator, UkOptionTradeCalculator>();
builder.Services.AddSingleton<ITradeCalculator, UkTradeCalculator>();
builder.Services.AddSingleton<ITradeCalculator, UkFutureTradeCalculator>();

// Register any new broker parsers here in order of priority
builder.Services.AddSingleton<ITaxEventFileParser, IBParseController>();
builder.Services.AddSingleton<ITaxEventFileParser, JsonParseController>();

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
builder.Services.AddSingleton<ResidencyStatusRecord>();
builder.Services.AddSingleton<TradeTaxCalculationFactory>();

//ViewModels
builder.Services.AddSingleton<InputGridDatas>();
builder.Services.AddSingleton<DividendToIncomeConvertViewModel>();

builder.Services.AddSyncfusionBlazor();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
WebAssemblyHost hostInstance = builder.Build();
CustomFontResolver fontResolver = new(hostInstance.Services.GetRequiredService<HttpClient>());
await fontResolver.InitializeFontsAsync();
GlobalFontSettings.FontResolver = fontResolver;
await hostInstance.RunAsync();
