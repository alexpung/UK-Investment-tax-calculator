using CsvHelper;
using CsvHelper.Configuration;

using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Globalization;
using System.Text.RegularExpressions;

namespace InvestmentTaxCalculator.Parser.InteractiveInvestorCsv;

/// <summary>
/// Parses the transaction history CSV downloaded from interactive investor (ii.co.uk)
/// via Portfolio -> Transaction history -> download CSV.
/// The export is a cash ledger: each row has a Debit or Credit amount in GBP.
/// Only stock/ETF/fund trades, dividends and cash interest are imported; other row
/// types (deposits, withdrawals, fees, transfers) are ignored.
/// Note: trade Debit/Credit amounts are all-in cash movements, so dealing charges are
/// already included in an acquisition cost / netted off a disposal proceed.
/// </summary>
public partial class InteractiveInvestorCsvParseController : IColumnMappableCsvParser
{
    // Logical column keys, mapped to the actual file headers via CsvColumnMap
    private const string _dateName = "Date";
    private const string _settlementDateName = "Settlement Date";
    private const string _symbolName = "Symbol";
    private const string _sedolName = "Sedol";
    private const string _quantityName = "Quantity";
    private const string _priceName = "Price";
    private const string _descriptionName = "Description";
    private const string _debitName = "Debit";
    private const string _creditName = "Credit";

    public string FormatName => "interactive investor";

    // Settlement Date, Sedol and Price are not read but only used to recognise the file format.
    public IReadOnlyList<CsvColumnDefinition> Columns { get; } =
    [
        new(_dateName, "Trade date (dd/MM/yyyy)", true, ["Date"]),
        new(_symbolName, "Ticker symbol", true, ["Symbol"]),
        new(_quantityName, "Number of shares traded", true, ["Quantity"]),
        new(_descriptionName, "Transaction description", true, ["Description"]),
        new(_debitName, "Cash paid out in GBP", true, ["Debit"]),
        new(_creditName, "Cash received in GBP", true, ["Credit"]),
        new(_settlementDateName, "Settlement date", false, ["Settlement Date"]),
        new(_sedolName, "SEDOL of the instrument", false, ["Sedol"]),
        new(_priceName, "Price per share", false, ["Price"]),
    ];

    private static readonly string[] _acceptedDateFormats = ["dd/MM/yyyy", "dd-MM-yyyy", "dd/MM/yy"];

    [GeneratedRegex(@"\bDIV(IDEND)?S?\b", RegexOptions.IgnoreCase)]
    private static partial Regex DividendRegex();

    [GeneratedRegex(@"\bINTEREST\b", RegexOptions.IgnoreCase)]
    private static partial Regex InterestRegex();

    private readonly CsvConfiguration _config = new(CultureInfo.InvariantCulture)
    {
        HeaderValidated = null,
        MissingFieldFound = null
    };

    public TaxEventLists ParseFile(string data) => ParseFile(data, CsvColumnMapper.AutoMapOrThrow(this, data));

    public TaxEventLists ParseFile(string data, CsvColumnMap columnMap)
    {
        var taxEvents = new TaxEventLists();
        using var reader = new StringReader(data);
        using var csv = new CsvReader(reader, _config);
        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            string symbol = csv.GetField(columnMap[_symbolName])?.Trim() ?? string.Empty;
            string description = csv.GetField(columnMap[_descriptionName])?.Trim() ?? string.Empty;
            decimal? quantity = ParseNullableDecimal(csv.GetField(columnMap[_quantityName]));
            decimal? debit = ParseNullableDecimal(csv.GetField(columnMap[_debitName]));
            decimal? credit = ParseNullableDecimal(csv.GetField(columnMap[_creditName]));
            DateTime date = ParseDate(csv, columnMap[_dateName]);

            if (IsTradeRow(symbol, quantity, debit, credit))
            {
                taxEvents.Trades.Add(ParseTrade(symbol, description, quantity!.Value, debit, credit, date));
            }
            else if (IsDividendRow(symbol, description, credit))
            {
                taxEvents.Dividends.Add(ParseDividend(symbol, description, credit!.Value, date));
            }
            else if (IsInterestRow(description, credit))
            {
                taxEvents.InterestIncomes.Add(ParseInterest(description, credit!.Value, date));
            }
            // All other row types (deposits, withdrawals, fees, transfers...) are ignored.
        }
        return taxEvents;
    }

    private static bool IsTradeRow(string symbol, decimal? quantity, decimal? debit, decimal? credit)
        => symbol != string.Empty && quantity is > 0 && (debit is > 0 ^ credit is > 0);

    private static bool IsDividendRow(string symbol, string description, decimal? credit)
        => symbol != string.Empty && credit is > 0 && DividendRegex().IsMatch(description);

    private static bool IsInterestRow(string description, decimal? credit)
        => credit is > 0 && InterestRegex().IsMatch(description);

    private static Trade ParseTrade(string symbol, string description, decimal quantity, decimal? debit, decimal? credit, DateTime date)
    {
        // Money out (debit) = acquisition, money in (credit) = disposal.
        // The cash movement already includes dealing charges and stamp duty, which is the
        // correct all-in allowable cost / net disposal proceed for CGT purposes.
        bool isAcquisition = debit is > 0;
        return new Trade
        {
            AssetName = symbol,
            Quantity = quantity,
            GrossProceed = new DescribedMoney(isAcquisition ? debit!.Value : credit!.Value, WrappedMoney.BaseCurrency, 1, description),
            Date = date,
            AcquisitionDisposal = isAcquisition ? TradeType.ACQUISITION : TradeType.DISPOSAL
        };
    }

    private static Dividend ParseDividend(string symbol, string description, decimal credit, DateTime date)
    {
        // The ii export has no ISIN so the company location cannot be derived.
        // It is left as unknown for the user to review, e.g. for foreign withholding tax.
        return new Dividend
        {
            AssetName = symbol,
            Proceed = new DescribedMoney(credit, WrappedMoney.BaseCurrency, 1, description),
            Date = date,
            DividendType = DividendType.DIVIDEND,
            CompanyLocation = CountryCode.UnknownRegion
        };
    }

    private static InterestIncome ParseInterest(string description, decimal credit, DateTime date)
    {
        return new InterestIncome
        {
            AssetName = "Broker interest",
            Amount = new DescribedMoney(credit, WrappedMoney.BaseCurrency, 1, description),
            InterestType = InterestType.SAVINGS,
            Date = date
        };
    }

    private static DateTime ParseDate(CsvReader csv, string dateHeader)
    {
        // ii is a GB platform: dates are day-first, so they are parsed with explicit dd/MM formats
        // rather than culture-dependent parsing that could read them as MM/dd.
        string dateString = csv.GetFieldSafe(dateHeader);
        if (DateTime.TryParseExact(dateString, _acceptedDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
        {
            return date;
        }
        throw new InvalidDataException($"Invalid date '{dateString}' on row {csv.Context.Parser?.Row}, expected one of: {string.Join(", ", _acceptedDateFormats)}.");
    }

    private static decimal? ParseNullableDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        string cleaned = value.Replace(",", "").Replace("£", "").Trim();
        if (decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result))
        {
            return result;
        }
        return null; // e.g. "n/a" placeholders on cash rows
    }

    public bool CheckFileValidity(string data, string contentType)
    {
        if (!CsvParserHelper.IsCsvContentType(contentType)) return false;
        try
        {
            // The headers are fairly generic, so all of them must be present to recognise the file automatically.
            CsvColumnMap map = CsvColumnMapper.AutoMap(Columns, CsvColumnMapper.ReadHeaders(data));
            return Columns.All(column => map.TryGetHeader(column.Key, out _));
        }
        catch
        {
            return false;
        }
    }
}
