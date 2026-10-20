using CsvHelper;
using CsvHelper.Configuration;

using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Globalization;

namespace InvestmentTaxCalculator.Parser.Trading212Csv;

public class Trading212CsvParseController : IColumnMappableCsvParser
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

    public string FormatName => "Trading 212";

    public IReadOnlyList<CsvColumnDefinition> Columns { get; } =
    [
        new(_actionName, "Transaction type, e.g. Market buy, Dividend (Ordinary), Interest on cash", true, ["Action"]),
        new(_timeName, "Date and time of the transaction", true, ["Time", "Time (UTC)"]),
        new(_tickerName, "Ticker symbol", true, ["Ticker"]),
        new(_isinName, "ISIN of the instrument", true, ["ISIN"]),
        new(_quantityName, "Number of shares traded", true, ["No. of shares"]),
        new(_totalGbpName, "Total cash amount of the transaction in GBP", true, ["Total (GBP)", "Total"]),
        new(_totalCurrencyName, "Currency of the total amount, which must be GBP", false, ["Currency (Total)"]),
        new(_nameName, "Instrument name, used to describe dividends", false, ["Name"]),
        new(_withholdingTaxName, "Withholding tax deducted from dividends", false, ["Withholding tax"]),
    ];

    private readonly CsvConfiguration _config = new(CultureInfo.InvariantCulture)
    {
        HeaderValidated = null,
        MissingFieldFound = args => throw new InvalidDataException($"Field missing at index {args.Index} on row {args.Context.Parser?.Row}")
    };

    public TaxEventLists ParseFile(string data) => ParseFile(data, CsvColumnMapper.AutoMapOrThrow(this, data));

    public TaxEventLists ParseFile(string data, CsvColumnMap columnMap)
    {
        var trades = new TaxEventLists();
        using var reader = new StringReader(data);
        using var csv = new CsvReader(reader, _config);
        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            string action = csv.GetFieldSafe(columnMap[_actionName]);

            // Determine transaction type based on Action field
            if (action.Contains("buy", StringComparison.OrdinalIgnoreCase) ||
                action.Contains("sell", StringComparison.OrdinalIgnoreCase))
            {
                trades.Trades.Add(ParseTrade(csv, columnMap, action));
            }
            else if (action.Contains("dividend", StringComparison.OrdinalIgnoreCase))
            {
                ParseDividend(csv, columnMap, trades);
            }
            else if (action.Equals("Interest on cash", StringComparison.OrdinalIgnoreCase))
            {
                trades.InterestIncomes.Add(ParseInterest(csv, columnMap));
            }
            // Ignore other action types (deposits, withdrawals, etc.)
        }
        return trades;
    }

    private static decimal GetTotalGbp(CsvReader csv, CsvColumnMap columnMap)
    {
        // Newer exports have a plain "Total" column with its currency in "Currency (Total)" instead of "Total (GBP)".
        if (columnMap.TryGetHeader(_totalCurrencyName, out string? currencyHeader))
        {
            string? currency = csv.GetField(currencyHeader);
            if (!string.IsNullOrWhiteSpace(currency) && !currency.Trim().Equals(WrappedMoney.BaseCurrency, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Total on row {csv.Context.Parser?.Row} is in {currency}, only {WrappedMoney.BaseCurrency} totals are supported.");
            }
        }
        return csv.GetFieldSafe<decimal>(columnMap[_totalGbpName]);
    }

    private static DateTime GetTime(CsvReader csv, CsvColumnMap columnMap)
        => DateTimeOffset.Parse(csv.GetFieldSafe(columnMap[_timeName]), CultureInfo.InvariantCulture).DateTime;

    private static Trade ParseTrade(CsvReader csv, CsvColumnMap columnMap, string action)
    {
        decimal totalGbp = GetTotalGbp(csv, columnMap);
        decimal quantity = csv.GetFieldSafe<decimal>(columnMap[_quantityName]);
        string ticker = csv.GetFieldSafe(columnMap[_tickerName]);
        DateTime date = GetTime(csv, columnMap);

        // Determine if buy or sell
        TradeType tradeType = action.Contains("buy", StringComparison.OrdinalIgnoreCase)
            ? TradeType.ACQUISITION
            : TradeType.DISPOSAL;

        // Total (GBP) is negative for buys, positive for sells in Trading212 CSV
        // We need absolute value for GrossProceed
        decimal grossProceedAmount = Math.Abs(totalGbp);
        string isin = csv.GetFieldSafe(columnMap[_isinName]);

        return new Trade
        {
            AssetName = ticker,
            Quantity = quantity,
            GrossProceed = new DescribedMoney(grossProceedAmount, WrappedMoney.BaseCurrency, 1),
            Date = date,
            AcquisitionDisposal = tradeType,
            Isin = isin
        };
    }

    private static void ParseDividend(CsvReader csv, CsvColumnMap columnMap, TaxEventLists trades)
    {
        decimal totalGbp = GetTotalGbp(csv, columnMap);
        string ticker = csv.GetFieldSafe(columnMap[_tickerName]);
        string name = columnMap.TryGetHeader(_nameName, out string? nameHeader) ? csv.GetFieldSafe(nameHeader) : ticker;
        DateTime date = GetTime(csv, columnMap);
        string isin = csv.GetFieldSafe(columnMap[_isinName]);

        // Add dividend income
        trades.Dividends.Add(new Dividend
        {
            AssetName = ticker,
            Proceed = new DescribedMoney(totalGbp, WrappedMoney.BaseCurrency, 1, $"{name} dividend"),
            Date = date,
            DividendType = DividendType.DIVIDEND,
            CompanyLocation = CountryCode.GetRegionByTwoDigitCode(isin[..2]),
            Isin = isin
        });

        // Parse withholding tax if present
        if (columnMap.TryGetHeader(_withholdingTaxName, out string? withholdingTaxHeader) &&
            csv.TryGetField<decimal>(withholdingTaxHeader, out decimal withholdingTax) && withholdingTax != 0)
        {
            trades.Dividends.Add(new Dividend
            {
                AssetName = ticker,
                Proceed = new DescribedMoney(Math.Abs(withholdingTax), WrappedMoney.BaseCurrency, 1, $"{name} withholding tax"),
                Date = date,
                DividendType = DividendType.WITHHOLDING,
                CompanyLocation = CountryCode.GetRegionByTwoDigitCode(isin[..2]),
                Isin = isin
            });
        }
    }

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
}
