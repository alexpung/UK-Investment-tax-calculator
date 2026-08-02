# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A UK capital gains / dividend / interest tax calculator, shipped as a client-only Blazor WebAssembly app (no backend — all parsing and calculation happens in the browser). It parses broker export files (Interactive Brokers XML, FreeTrade CSV, Trading212 CSV, or a previously-exported JSON session) into a common set of tax events, then applies UK tax rules (TCGA92 same-day/bed-and-breakfast/Section 104 matching, ERI/equalisation for offshore funds, etc.) to produce disposal, dividend, and interest reports, optionally exported to PDF.

Live app: https://alexpung.github.io/UK-Investment-tax-calculator/

## Solution layout

- `BlazorApp-Investment Tax Calculator/InvestmentTaxCalculator.csproj` — the Blazor WASM app (target `net10.0`). Root namespace `InvestmentTaxCalculator`.
- `UnitTest/UnitTest.csproj` — xUnit unit tests, references the app project directly.
- `PlaywrightTests/PlaywrightTests.csproj` — NUnit + Playwright end-to-end browser tests (target `net10.0-windows...`, Windows-only).
- `CapitalGainCalculator.sln` ties the three together.
- `TaxExamples/` — sample broker files (IB XML, options, futures) and matching expected text output, used as fixtures by the unit tests (copied into test output via `<None Update>` in `UnitTest.csproj`).

## Common commands

Build and test from the repo root (all paths with spaces need quoting):

```powershell
dotnet build --configuration Release
dotnet test UnitTest/UnitTest.csproj
dotnet test UnitTest/UnitTest.csproj --filter "FullyQualifiedName~UkTradeCalculatorStockSplitTest"
dotnet test PlaywrightTests/PlaywrightTests.csproj --configuration Release
dotnet run --project "BlazorApp-Investment Tax Calculator/InvestmentTaxCalculator.csproj"
```

Notes:
- The app requires the `wasm-tools` workload (`dotnet workload install wasm-tools`) to build/publish.
- Playwright tests spin up the Blazor app themselves via `dotnet run` (see `PlaywrightTests/BlazorAppFixture.cs`) unless a `BASE_URL` env var points at an already-running instance (CI does this). They require Playwright browsers installed (`playwright.ps1 install --with-deps`) and are Windows-only.
- CI (`.github/workflows/main.yml`) runs on `windows-latest`: build → unit tests (with coverage) → Playwright tests → publish with `-p:GHPages=true` → deploy `dist/Web/wwwroot` to GitHub Pages on push to `master`.
- Unit tests use xUnit + Shouldly (assertions) + NSubstitute (mocking).

## Architecture

### Pipeline: parse → calculate → report

1. **Parse**: Each broker format has a controller implementing `ITaxEventFileParser` (`CheckFileValidity` + `ParseFile`), registered in `Program.cs` DI as `ITaxEventFileParser`. `FileParseController` (Parser/) picks the first parser whose `CheckFileValidity` matches an uploaded file's content, and merges results into a shared `TaxEventLists`. New broker support = new folder under `Parser/` implementing this interface and registering it in `Program.cs` (comment there notes registration order = priority). IB XML parsing lives under `Parser/InteractiveBrokersXml/`, one file per statement section (dividends, trades, splits, Fx, options, futures, cash settlements, interest).

2. **Model**: `TaxEventLists` (Model/TaxEventLists.cs) is the app-wide singleton holding raw parsed data, split by event type (`Trades`, `CorporateActions`, `Dividends`, `OptionTrades`, `FutureContractTrades`, `CashSettlements`, `InterestIncomes`). It also handles duplicate-import detection via each event's `GetDuplicateSignature()`.

3. **Calculate**: `TaxCalculationService` (Services/) orchestrates calculation: clears `UkSection104Pools`, resets `ITradeTaxCalculation` IDs, then runs every registered `ITradeCalculator` (order matters — see the comment in `Program.cs`: options must be calculated before instruments derived from them) followed by the single `IDividendCalculator`. Results land in the singleton `TradeCalculationResult` / `DividendCalculationResult` objects that views bind to.

4. **UK tax rules** live in `Model/UkTaxModel/`:
   - `UkMatchingRules` implements the core TCGA92 matching algorithm: same-day matching, 30-day bed-and-breakfast matching, then chronological Section 104 pooling / short-sale cover matching (`ApplyUkTaxRuleSequence` is the entry point most calculators call). Matching runs per-asset-name, using `GroupedTradeContainer<T>` to group and sort trades.
   - `UkTradeCalculator` (stocks), `UkFutureTradeCalculator`, `UkOptionTradeCalculator` implement `ITradeCalculator` per instrument type and each produce `ITradeTaxCalculation` results (`TradeTaxCalculation`, `FutureTradeTaxCalculation`, `OptionTradeTaxCalculation`).
   - `UkSection104`/`UkSection104Pools` track the running Section 104 pool (average cost) per asset; corporate actions that affect the pool implement `IChangeSection104`.
   - Long/short positions are reclassified chronologically by `UkMatchingRules.TagTradesWithOpenClose` (splits a trade in two when a single order flips a position from long to short or vice versa).
   - `UkDividendCalculator` handles dividend/interest/ERI income, including region detection from ISIN for Excess Reportable Income and Section 104 cost-base adjustment.
   - Non-UK tax logic could in principle be swapped in here — `Program.cs` comments call out the UK-specific service registrations as the place to change.

5. **Corporate actions**: modelled as `TaxEvents/CorporateAction` subclasses (`StockSplit`, `TakeoverCorporateAction`, `SpinoffCorporateAction`, `PartnerTransferCorporateAction`, `ReturnOfCapitalCorporateAction`), most user-entered manually through the UI (Components/`Takeover.razor`, `Spinoff.razor`, `StockSplit.razor`, `PartnerTransfer.razor`) since brokers rarely report the tax-relevant details (e.g. cost allocation) directly.

6. **Export/report**: `Services/` contains export/report services per output (dividend summary, trade calculation results, Section 104 history, PDF via `Services/PdfExport/`, which is a `MigraDoc`-based section-by-section PDF builder — see `Services/PdfExport/Sections/`). `Services/ObjectDetailsToPrintedString.cs` and each model's `ITextFilePrintable` implementation drive the human-readable text output shown in the README examples.

### UI layer

Standard Blazor WASM: `Pages/*.razor` are routed pages, `Components/*.razor` are reusable pieces, `ViewModel/*` are plain classes bound to from pages/components (not a strict MVVM framework — just DI-registered state holders like `InputGridDatas`). UI uses **Radzen.Blazor** components (migrated from Syncfusion — see recent commit history; don't reintroduce Syncfusion). `ToastService` is the app-wide notification/error-surface mechanism; most services report failures via `toastService.ShowException`/`ShowError` rather than throwing to the UI.

Most app state is registered as DI singletons in `Program.cs` (not scoped per-request, since this is a single-user WASM app) — when adding new cross-page state, follow that pattern rather than introducing new state-management approaches.

### Money/currency handling

`WrappedMoney` (Model/WarppedMoney.cs, note the existing filename typo) wraps `NMoneys.Money` and is the type used throughout for amounts — always in GBP for tax calculation purposes; original-currency amounts are tracked alongside via `DescribedMoney` plus an FX rate. `CurrencyService` handles currency-related conversions/formatting.

### Testing conventions

- `UnitTest/Test/TradeCalculations/{Stocks,Options,Futures}` — matching-rule/calculation correctness tests, generally structured as "given a specific trade history, assert the matches and gains produced" (see file names like `UkTradeCalculatorTest2Trade.cs`, `UkTradeCalculatorStockSplitTest.cs`).
- `UnitTest/Test/Parser` — per-broker-format parsing tests, often against fixtures in `TaxExamples/` or `UnitTest/Test/resource/`.
- `UnitTest/Test/Reproduction` — regression tests reproducing specific reported bugs.
- `PlaywrightTests/*Tests.cs` — end-to-end UI workflow tests (import → calculate → view results) driven through an actual running instance of the app.

Whenever a PR is created, the version in InvestmentTaxCalculator.csproj should also be bumped with the appropriate level according to what is changed.
