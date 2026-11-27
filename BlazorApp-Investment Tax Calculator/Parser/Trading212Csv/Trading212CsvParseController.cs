using CsvHelper;
using CsvHelper.Configuration;

using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Services;

using System.Globalization;

namespace InvestmentTaxCalculator.Parser.Trading212Csv;

/// <summary>
/// Parses the Trading 212 Invest account history CSV export.
/// Trading 212 publishes no specification for this file, so the behaviour here follows what is known from real exports:
/// <list type="bullet">
/// <item>Columns are only present when the exported period has data for them and their order changes, so columns are found by header.</item>
/// <item>Times are in UTC, both in the older "Time" and the newer "Time (UTC)" column, and are converted to UK local time.</item>
/// <item>A dividend and its withholding tax share one row. "Total" (account currency) and "Price / share" (instrument currency)
/// are both net of withholding tax, while "Withholding tax" has its own currency, usually the instrument currency.</item>
/// </list>
/// </summary>
public class Trading212CsvParseController(ToastService? toastService = null) : IColumnMappableCsvParser
{
    // Logical column keys, mapped to the actual file headers via CsvColumnMap
    private const string _actionName = "Action";
    private const string _timeName = "Time";
    private const string _tickerName = "Ticker";
    private const string _nameName = "Name";
    private const string _isinName = "ISIN";
    private const string _quantityName = "No. of shares";
    private const string _totalGbpName = "Total (GBP)";
    private const string _totalCurrencyName = "Currency (Total)";

    // Dividend-specific columns
    private const string _withholdingTaxName = "Withholding tax";
    private const string _withholdingTaxCurrencyName = "Currency (Withholding tax)";
    private const string _pricePerShareName = "Price / share";
    private const string _priceCurrencyName = "Currency (Price / share)";

    private const string _penceCurrency = "GBX";

    // Actions with no tax consequence, which are skipped without a warning.
    private static readonly string[] _nonTaxActionPrefixes =
        ["Deposit", "Withdrawal", "Currency conversion", "Card ", "New card cost", "ATM", "Spending cashback", "Result adjustment"];

    private static readonly Lazy<TimeZoneInfo> _ukTimeZone = new(FindUkTimeZone);

    public string FormatName => "Trading 212";

    public IReadOnlyList<CsvColumnDefinition> Columns { get; } =
    [
        new(_actionName, "Transaction type, e.g. Market buy, Dividend (Ordinary), Interest on cash", true, ["Action"]),
        new(_timeName, "Date and time of the transaction in UTC", true, ["Time", "Time (UTC)"]),
        new(_tickerName, "Ticker symbol", true, ["Ticker"]),
        new(_isinName, "ISIN of the instrument", true, ["ISIN"]),
        new(_quantityName, "Number of shares traded, or the number of shares a dividend was paid on", true, ["No. of shares"]),
        new(_totalGbpName, "Total cash amount of the transaction in GBP, net of withholding tax for dividends", true, ["Total (GBP)", "Total"]),
        new(_totalCurrencyName, "Currency of the total amount, which must be GBP", false, ["Currency (Total)"]),
        new(_nameName, "Instrument name, used to describe dividends", false, ["Name"]),
        new(_withholdingTaxName, "Withholding tax deducted from dividends", false, ["Withholding tax"]),
        new(_withholdingTaxCurrencyName, "Currency of the withholding tax, if blank or unmapped it is assumed to be GBP", false, ["Currency (Withholding tax)"]),
        new(_pricePerShareName, "Price per share, or the net dividend per share for dividends, used to convert foreign withholding tax", false, ["Price / share"]),
        new(_priceCurrencyName, "Currency of the price per share", false, ["Currency (Price / share)"]),
    ];

    private readonly CsvConfiguration _config = new(CultureInfo.InvariantCulture)
    {
        HeaderValidated = null,
        MissingFieldFound = args => throw new InvalidDataException($"Field missing at index {args.Index} on row {args.Context.Parser?.Row}")
    };

    public TaxEventLists ParseFile(string data) => ParseFile(data, CsvColumnMapper.AutoMap(Columns, CsvColumnMapper.ReadHeaders(data)));

    public IReadOnlyList<string> ValidateColumnMap(string data, CsvColumnMap columnMap)
    {
        List<string> problems = CsvColumnMapper.GetMissingRequiredColumnMessages(Columns, columnMap);
        // Only the legacy "Total (GBP)" header states its currency; any other total column needs its currency column to check it is GBP.
        if (columnMap.TryGetHeader(_totalGbpName, out string? totalHeader) &&
            !totalHeader.Trim().Equals(_totalGbpName, StringComparison.OrdinalIgnoreCase) &&
            !columnMap.TryGetHeader(_totalCurrencyName, out _))
        {
            problems.Add($"Column '{_totalCurrencyName}' must be mapped to check the total column '{totalHeader}' is in {WrappedMoney.BaseCurrency.ToUpperInvariant()}.");
        }
        return problems;
    }

    public TaxEventLists ParseFile(string data, CsvColumnMap columnMap)
    {
        CsvColumnMapper.ThrowIfInvalid(this, data, columnMap);
        var trades = new TaxEventLists();
        var warnings = new ParseWarnings();
        using var reader = new StringReader(data);
        using var csv = new CsvReader(reader, _config);
        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            string action = csv.GetFieldSafe(columnMap[_actionName]).Trim();

            if (action.StartsWith("Dividend (", StringComparison.OrdinalIgnoreCase))
            {
                ParseDividend(csv, columnMap, action, trades, warnings);
            }
            else if (action.Equals("Interest on cash", StringComparison.OrdinalIgnoreCase))
            {
                trades.InterestIncomes.Add(ParseInterest(csv, columnMap));
            }
            else if (IsTradeAction(action))
            {
                Trade? trade = ParseTrade(csv, columnMap, action, warnings);
                if (trade is not null) trades.Trades.Add(trade);
            }
            else if (!_nonTaxActionPrefixes.Any(prefix => action.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                // e.g. Dividend adjustment, Lending interest, Stock split open/close, Spin off, Stock distribution, Transfer in/out, ADR Fee
                warnings.Add($"'{action}' rows are not imported and may need to be entered manually");
            }
        }
        warnings.Show(toastService);
        return trades;
    }

    // Market/Limit/Stop/Stop limit buy and sell
    private static bool IsTradeAction(string action) =>
        action.EndsWith("buy", StringComparison.OrdinalIgnoreCase) || action.EndsWith("sell", StringComparison.OrdinalIgnoreCase);

    private static decimal GetTotalGbp(CsvReader csv, CsvColumnMap columnMap)
    {
        // Newer exports have a plain "Total" column with its currency in "Currency (Total)" instead of "Total (GBP)".
        if (columnMap.TryGetHeader(_totalCurrencyName, out string? currencyHeader))
        {
            string? currency = csv.GetField(currencyHeader);
            if (!string.IsNullOrWhiteSpace(currency) && !currency.Trim().Equals(WrappedMoney.BaseCurrency, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Total on row {csv.Context.Parser?.Row} is in {currency}, only {WrappedMoney.BaseCurrency.ToUpperInvariant()} totals are supported.");
            }
        }
        return csv.GetFieldSafe<decimal>(columnMap[_totalGbpName]);
    }

    /// <summary>
    /// Trading 212 times are UTC (e.g. "2024-01-15 10:30:00" or "2026-07-24 01:16:11+00:00"). Tax years and the same day /
    /// 30 day matching rules use UK calendar days, so the time is converted to UK local time.
    /// </summary>
    private static DateTime GetTime(CsvReader csv, CsvColumnMap columnMap)
    {
        string time = csv.GetFieldSafe(columnMap[_timeName]);
        if (!DateTimeOffset.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset utcTime))
        {
            throw new InvalidDataException($"Invalid time '{time}' on row {csv.Context.Parser?.Row}.");
        }
        return TimeZoneInfo.ConvertTime(utcTime, _ukTimeZone.Value).DateTime;
    }

    private static TimeZoneInfo FindUkTimeZone()
    {
        foreach (string id in new[] { "Europe/London", "GMT Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out TimeZoneInfo? timeZone)) return timeZone;
        }
        // Without time zone data, fall back to the GMT/BST rule (last Sunday of March to last Sunday of October at 01:00 UTC).
        var bstRule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(DateTime.MinValue.Date, DateTime.MaxValue.Date, TimeSpan.FromHours(1),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 1, 0, 0), 3, 5, DayOfWeek.Sunday),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 10, 5, DayOfWeek.Sunday));
        return TimeZoneInfo.CreateCustomTimeZone("UK", TimeSpan.Zero, "UK", "GMT", "BST", [bstRule]);
    }

    private static Trade? ParseTrade(CsvReader csv, CsvColumnMap columnMap, string action, ParseWarnings warnings)
    {
        decimal totalGbp = GetTotalGbp(csv, columnMap);
        decimal quantity = csv.GetFieldSafe<decimal>(columnMap[_quantityName]);
        string ticker = csv.GetFieldSafe(columnMap[_tickerName]);
        DateTime date = GetTime(csv, columnMap);

        TradeType tradeType = action.EndsWith("buy", StringComparison.OrdinalIgnoreCase) ? TradeType.ACQUISITION : TradeType.DISPOSAL;
        if (totalGbp == 0 && tradeType == TradeType.DISPOSAL)
        {
            // A sell at zero is how Trading 212 records shares exchanged in a takeover; it is not a disposal for nothing.
            // A zero-cost acquisition is kept, as dropping it would leave too few shares for later disposals.
            warnings.Add($"Sells with a zero total (usually shares exchanged in a takeover) are not imported, enter the corporate action manually instead");
            return null;
        }

        // Total is negative for buys, positive for sells and already includes fees (FX fee, stamp duty, French transaction tax),
        // so it is the allowable cost or the net proceeds.
        return new Trade
        {
            AssetName = ticker,
            Quantity = quantity,
            GrossProceed = new DescribedMoney(Math.Abs(totalGbp), WrappedMoney.BaseCurrency, 1),
            Date = date,
            AcquisitionDisposal = tradeType,
            // ISIN is optional; trades without one are matched by ticker.
            Isin = csv.GetFieldOrEmpty(columnMap[_isinName])
        };
    }

    private static void ParseDividend(CsvReader csv, CsvColumnMap columnMap, string action, TaxEventLists trades, ParseWarnings warnings)
    {
        decimal netGbp = GetTotalGbp(csv, columnMap);
        string ticker = csv.GetFieldSafe(columnMap[_tickerName]);
        string name = GetOptionalField(csv, columnMap, _nameName);
        if (name == string.Empty) name = ticker;
        DateTime date = GetTime(csv, columnMap);
        // A blank ISIN should not reject the whole file; the region is then recorded as unknown
        // and the user is warned to set it manually.
        string isin = csv.GetFieldOrEmpty(columnMap[_isinName]);
        CountryCode location = CountryCode.GetRegionByIsin(isin);
        string label = action["Dividend (".Length..].TrimEnd(')').Trim();

        Withholding? withholding = GetWithholding(csv, columnMap, netGbp);
        decimal withholdingGbp = withholding?.Gbp ?? 0;
        string description = $"{name} {label}: {DescribeNetPerShare(csv, columnMap)}{(withholding is null ? "" : $", {withholding.Amount} {withholding.Currency} withheld")}";

        if (label.Contains("interest", StringComparison.OrdinalIgnoreCase))
        {
            // An interest distribution from a bond fund is savings income, not a dividend. Like dividends it is recorded gross,
            // as Total is net of any withholding tax.
            trades.InterestIncomes.Add(new InterestIncome
            {
                AssetName = ticker,
                Amount = new DescribedMoney(netGbp + withholdingGbp, WrappedMoney.BaseCurrency, 1, description),
                InterestType = InterestType.ETFDIVIDEND,
                IncomeLocation = location,
                Date = date,
                Isin = isin
            });
            if (withholding is not null) warnings.Add("Withholding tax on interest distributions is not recorded as foreign tax paid");
            return;
        }

        if (label.Contains("return of capital", StringComparison.OrdinalIgnoreCase))
        {
            // A return of capital is not income: it reduces the allowable cost of the shares (TCGA92 s122).
            trades.CorporateActions.Add(new ReturnOfCapitalCorporateAction
            {
                AssetName = ticker,
                Date = date,
                Amount = new DescribedMoney(netGbp + withholdingGbp, WrappedMoney.BaseCurrency, 1, description),
                Isin = isin
            });
            return;
        }

        if (label.Contains("property income", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Property income distributions (REIT PIDs) are imported as dividends but are taxed as property income, " +
                "and tax deducted from them is UK tax rather than foreign tax");
        }
        if (label.Contains("tax exempt", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("'Tax exempted' dividends are imported as dividends, check whether any are a return of capital instead");
        }

        // Record the gross dividend and the withholding tax separately, matching the Interactive Brokers convention.
        trades.Dividends.Add(new Dividend
        {
            AssetName = ticker,
            Proceed = new DescribedMoney(netGbp + withholdingGbp, WrappedMoney.BaseCurrency, 1, description),
            Date = date,
            DividendType = label.Contains("manufactured", StringComparison.OrdinalIgnoreCase) ? DividendType.DIVIDEND_IN_LIEU : DividendType.DIVIDEND,
            CompanyLocation = location,
            Isin = isin
        });

        if (withholding is not null)
        {
            trades.Dividends.Add(new Dividend
            {
                AssetName = ticker,
                // Withholding tax is stored as a negative amount (tax paid), matching the Interactive Brokers convention.
                Proceed = new DescribedMoney(-withholding.Amount, withholding.Currency, withholding.FxRate, $"{name} withholding tax"),
                Date = date,
                DividendType = DividendType.WITHHOLDING,
                CompanyLocation = location,
                Isin = isin
            });
        }
    }

    /// <param name="Amount">Withholding tax in its own currency, positive.</param>
    /// <param name="FxRate">GBP per unit of <paramref name="Currency"/>.</param>
    private sealed record Withholding(decimal Amount, string Currency, decimal FxRate)
    {
        public decimal Gbp => Amount * FxRate;
    }

    /// <summary>
    /// Reads the withholding tax on a dividend row, with the rate to convert it to GBP. When it is in a foreign currency the rate
    /// is implied from the row itself: the net dividend in the instrument currency (shares x net dividend per share) was credited
    /// as the net total in GBP. This works for both older exports, where "Exchange rate" is "Not available" on dividend rows,
    /// and newer ones, and avoids relying on that column, whose direction on dividend rows is the reverse of trade rows.
    /// </summary>
    private static Withholding? GetWithholding(CsvReader csv, CsvColumnMap columnMap, decimal netGbp)
    {
        if (!TryGetOptionalDecimal(csv, columnMap, _withholdingTaxName, out decimal amount) || amount == 0) return null;
        amount = Math.Abs(amount);

        string currency = GetOptionalField(csv, columnMap, _withholdingTaxCurrencyName).ToUpperInvariant();
        if (currency == string.Empty || currency.Equals(WrappedMoney.BaseCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return new Withholding(amount, WrappedMoney.BaseCurrency, 1);
        }
        if (currency == _penceCurrency) return new Withholding(amount / 100, WrappedMoney.BaseCurrency, 1);

        string priceCurrency = GetOptionalField(csv, columnMap, _priceCurrencyName);
        if (priceCurrency.Equals(currency, StringComparison.OrdinalIgnoreCase) &&
            TryGetOptionalDecimal(csv, columnMap, _quantityName, out decimal shares) &&
            TryGetOptionalDecimal(csv, columnMap, _pricePerShareName, out decimal netPerShare) &&
            shares * netPerShare != 0 && netGbp != 0)
        {
            return new Withholding(amount, currency, Math.Abs(netGbp / (shares * netPerShare)));
        }

        throw new InvalidDataException($"Withholding tax on row {csv.Context.Parser?.Row} is in {currency} but cannot be converted to " +
            $"{WrappedMoney.BaseCurrency.ToUpperInvariant()}: the '{_quantityName}', '{_pricePerShareName}' and '{_priceCurrencyName}' columns " +
            $"are needed, with the price in {currency}.");
    }

    private static string DescribeNetPerShare(CsvReader csv, CsvColumnMap columnMap)
    {
        if (!TryGetOptionalDecimal(csv, columnMap, _quantityName, out decimal shares) ||
            !TryGetOptionalDecimal(csv, columnMap, _pricePerShareName, out decimal netPerShare)) return "dividend";
        return $"{shares:0.##########} shares x {netPerShare} {GetOptionalField(csv, columnMap, _priceCurrencyName)} net per share";
    }

    private static string GetOptionalField(CsvReader csv, CsvColumnMap columnMap, string key)
        => columnMap.TryGetHeader(key, out string? header) ? csv.GetFieldOrEmpty(header) : string.Empty;

    private static bool TryGetOptionalDecimal(CsvReader csv, CsvColumnMap columnMap, string key, out decimal value)
        => decimal.TryParse(GetOptionalField(csv, columnMap, key), NumberStyles.Number, CultureInfo.InvariantCulture, out value);

    private static InterestIncome ParseInterest(CsvReader csv, CsvColumnMap columnMap)
    {
        decimal totalGbp = GetTotalGbp(csv, columnMap);
        DateTime date = GetTime(csv, columnMap);

        return new InterestIncome
        {
            AssetName = "Broker interest",
            Amount = new DescribedMoney(totalGbp, WrappedMoney.BaseCurrency, 1, "Interest from cash account"),
            InterestType = InterestType.SAVINGS,
            Date = date
        };
    }

    public bool CheckFileValidity(string data, string contentType)
        => CsvParserHelper.IsCsvContentType(contentType) && CsvColumnMapper.CanAutoMap(Columns, data);

    /// <summary>Collects warnings while parsing so each kind is shown once, with how many rows it applies to.</summary>
    private sealed class ParseWarnings
    {
        private readonly Dictionary<string, int> _counts = [];

        public void Add(string message) => _counts[message] = _counts.GetValueOrDefault(message) + 1;

        public void Show(ToastService? toastService)
        {
            foreach ((string message, int count) in _counts)
            {
                toastService?.ShowWarning($"Trading 212: {message} ({count} row{(count == 1 ? "" : "s")}).");
            }
        }
    }
}
