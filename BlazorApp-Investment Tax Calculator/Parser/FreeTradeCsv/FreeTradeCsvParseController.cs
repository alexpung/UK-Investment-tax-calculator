using CsvHelper;
using CsvHelper.Configuration;

using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Globalization;

namespace InvestmentTaxCalculator.Parser.FreeTradeCsv;

public class FreeTradeCsvParseController : IColumnMappableCsvParser
{
    // Logical column keys, mapped to the actual file headers via CsvColumnMap
    private const string _totalAmountName = "Total Amount";
    private const string _tickerName = "Ticker";
    private const string _isinName = "ISIN";
    private const string _titleName = "Title";
    private const string _typeName = "Type";
    private const string _timeStampName = "Timestamp";
    private const string _quantityName = "Quantity";
    private const string _buySellName = "Buy / Sell";
    private const string _dividendPayDateName = "Dividend Pay Date";
    private const string _dividendPerShareName = "Dividend Amount Per Share";
    private const string _dividendWithheldTaxName = "Dividend Withheld Tax Amount";

    public string FormatName => "Freetrade";

    public IReadOnlyList<CsvColumnDefinition> Columns { get; } =
    [
        new(_typeName, "Row type, e.g. ORDER, DIVIDEND, INTEREST_FROM_CASH", true, ["Type"]),
        new(_timeStampName, "Date and time of the transaction", true, ["Timestamp"]),
        new(_tickerName, "Ticker symbol", true, ["Ticker"]),
        new(_isinName, "ISIN of the instrument", true, ["ISIN"]),
        new(_quantityName, "Number of shares traded", true, ["Quantity"]),
        new(_totalAmountName, "Total cash amount of the transaction in GBP", true, ["Total Amount"]),
        new(_buySellName, "BUY or SELL for orders", true, ["Buy / Sell"]),
        new(_titleName, "Instrument name, used to describe dividends", false, ["Title"]),
        new(_dividendPayDateName, "Payment date of dividends, needed if the file has dividends", false, ["Dividend Pay Date"]),
        new(_dividendPerShareName, "Dividend per share, used to describe dividends", false, ["Dividend Amount Per Share"]),
        new(_dividendWithheldTaxName, "Withholding tax deducted from dividends, needed if the file has dividends", false, ["Dividend Withheld Tax Amount"]),
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
        if (columnMap.TryGetHeader(_typeName, out string? typeHeader) && HasDividendRows(data, typeHeader))
        {
            foreach (string key in new[] { _dividendPayDateName, _dividendWithheldTaxName }.Where(key => !columnMap.TryGetHeader(key, out _)))
            {
                problems.Add($"Column '{key}' must be mapped as the file has dividends.");
            }
        }
        return problems;
    }

    private bool HasDividendRows(string data, string typeHeader)
    {
        using var reader = new StringReader(data);
        using var csv = new CsvReader(reader, _config);
        csv.Read();
        csv.ReadHeader();
        while (csv.Read())
        {
            if (csv.GetField(typeHeader) == "DIVIDEND") return true;
        }
        return false;
    }

    public TaxEventLists ParseFile(string data, CsvColumnMap columnMap)
    {
        CsvColumnMapper.ThrowIfInvalid(this, data, columnMap);
        var trades = new TaxEventLists();
        using var reader = new StringReader(data);
        using var csv = new CsvReader(reader, _config);
        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            string rowType = csv.GetFieldSafe(columnMap[_typeName]);
            switch (rowType)
            {
                case "ORDER":
                    trades.Trades.Add(new Trade
                    {
                        AssetName = csv.GetFieldSafe(columnMap[_tickerName]),
                        Quantity = csv.GetFieldSafe<decimal>(columnMap[_quantityName]),
                        GrossProceed = new DescribedMoney(csv.GetField<decimal>(columnMap[_totalAmountName]), WrappedMoney.BaseCurrency, 1),
                        Date = DateTimeOffset.Parse(csv.GetFieldSafe(columnMap[_timeStampName]), CultureInfo.InvariantCulture).DateTime,
                        AcquisitionDisposal = csv.GetFieldSafe(columnMap[_buySellName]).Equals("BUY", StringComparison.CurrentCultureIgnoreCase)
                               ? TradeType.ACQUISITION
                               : TradeType.DISPOSAL,
                        Isin = csv.GetFieldSafe(columnMap[_isinName])
                    });
                    break;
                case "DIVIDEND":
                    DateTime dividendDate = csv.ParseDateStringToDateTime(columnMap[_dividendPayDateName]);
                    trades.Dividends.Add(new Dividend
                    {
                        AssetName = csv.GetFieldSafe(columnMap[_tickerName]),
                        Proceed = new DescribedMoney(csv.GetField<decimal>(columnMap[_totalAmountName]), WrappedMoney.BaseCurrency, 1, $"{GetOptionalField(csv, columnMap, _titleName)} dividend: {GetOptionalField(csv, columnMap, _dividendPerShareName)} per share."),
                        Date = dividendDate,
                        DividendType = DividendType.DIVIDEND,
                        CompanyLocation = CountryCode.GetRegionByTwoDigitCode(csv.GetFieldSafe(columnMap[_isinName])[..2]),
                        Isin = csv.GetFieldSafe(columnMap[_isinName])
                    });
                    trades.Dividends.Add(new Dividend
                    {
                        AssetName = csv.GetFieldSafe(columnMap[_tickerName]),
                        Proceed = new DescribedMoney(csv.GetField<decimal>(columnMap[_dividendWithheldTaxName]), WrappedMoney.BaseCurrency, 1, $"{GetOptionalField(csv, columnMap, _titleName)} withholding tax"),
                        Date = dividendDate,
                        DividendType = DividendType.WITHHOLDING,
                        CompanyLocation = CountryCode.GetRegionByTwoDigitCode(csv.GetFieldSafe(columnMap[_isinName])[..2]),
                        Isin = csv.GetFieldSafe(columnMap[_isinName])
                    });

                    break;
                case "INTEREST_FROM_CASH":
                    trades.InterestIncomes.Add(new InterestIncome
                    {
                        AssetName = "Broker interest",
                        Amount = new DescribedMoney(csv.GetFieldSafe<decimal>(columnMap[_totalAmountName]), WrappedMoney.BaseCurrency, 1, "Interest from cash account."),
                        InterestType = InterestType.SAVINGS,
                        Date = DateTimeOffset.Parse(csv.GetFieldSafe(columnMap[_timeStampName]), CultureInfo.InvariantCulture).DateTime,
                    });
                    break;
                default:
                    break;
            }
        }
        return trades;
    }

    private static string? GetOptionalField(CsvReader csv, CsvColumnMap columnMap, string key)
        => columnMap.TryGetHeader(key, out string? header) ? csv.GetField(header) : null;

    public bool CheckFileValidity(string data, string contentType)
        => CsvParserHelper.IsCsvContentType(contentType) && CsvColumnMapper.CanAutoMap(Columns, data);
}
