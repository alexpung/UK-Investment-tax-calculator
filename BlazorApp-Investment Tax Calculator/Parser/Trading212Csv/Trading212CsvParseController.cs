using CsvHelper;
using CsvHelper.Configuration;

using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Collections.Immutable;
using System.Globalization;

namespace InvestmentTaxCalculator.Parser.Trading212Csv;

public class Trading212CsvParseController : ITaxEventFileParser
{
    // Core column names
    private const string _actionName = "Action";
    private const string _timeName = "Time";
    private const string _tickerName = "Ticker";
    private const string _nameName = "Name";
    private const string _isinName = "ISIN";
    private const string _quantityName = "No. of shares";
    private const string _totalGbpName = "Total (GBP)";

    // Dividend-specific columns
    private const string _withholdingTaxName = "Withholding tax";

    private readonly CsvConfiguration _config = new(CultureInfo.InvariantCulture)
    {
        HeaderValidated = null,
        MissingFieldFound = args => throw new InvalidDataException($"Field missing at index {args.Index} on row {args.Context.Parser?.Row}")
    };

    public TaxEventLists ParseFile(string data)
    {
        var trades = new TaxEventLists();
        using var reader = new StringReader(data);
        using var csv = new CsvReader(reader, _config);
        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            string action = csv.GetFieldSafe(_actionName);

            // Determine transaction type based on Action field
            if (action.Contains("buy", StringComparison.OrdinalIgnoreCase) ||
                action.Contains("sell", StringComparison.OrdinalIgnoreCase))
            {
                trades.Trades.Add(ParseTrade(csv, action));
            }
            else if (action.Contains("dividend", StringComparison.OrdinalIgnoreCase))
            {
                ParseDividend(csv, trades);
            }
            else if (action.Equals("Interest on cash", StringComparison.OrdinalIgnoreCase))
            {
                trades.InterestIncomes.Add(ParseInterest(csv));
            }
            // Ignore other action types (deposits, withdrawals, etc.)
        }
        return trades;
    }

    private static Trade ParseTrade(CsvReader csv, string action)
    {
        decimal totalGbp = csv.GetFieldSafe<decimal>(_totalGbpName);
        decimal quantity = csv.GetFieldSafe<decimal>(_quantityName);
        string ticker = csv.GetFieldSafe(_tickerName);
        DateTime date = DateTimeOffset.Parse(csv.GetFieldSafe(_timeName), CultureInfo.InvariantCulture).DateTime;

        // Determine if buy or sell
        TradeType tradeType = action.Contains("buy", StringComparison.OrdinalIgnoreCase)
            ? TradeType.ACQUISITION
            : TradeType.DISPOSAL;

        // Total (GBP) is negative for buys, positive for sells in Trading212 CSV
        // We need absolute value for GrossProceed
        decimal grossProceedAmount = Math.Abs(totalGbp);
        string isin = csv.GetFieldSafe(_isinName);

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

    private static void ParseDividend(CsvReader csv, TaxEventLists trades)
    {
        decimal totalGbp = csv.GetFieldSafe<decimal>(_totalGbpName);
        string ticker = csv.GetFieldSafe(_tickerName);
        string name = csv.GetFieldSafe(_nameName);
        DateTime date = DateTimeOffset.Parse(csv.GetFieldSafe(_timeName), CultureInfo.InvariantCulture).DateTime;
        string isin = csv.GetFieldSafe(_isinName);

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
        if (csv.TryGetField<decimal>(_withholdingTaxName, out decimal withholdingTax) && withholdingTax != 0)
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

    private static InterestIncome ParseInterest(CsvReader csv)
    {
        decimal totalGbp = csv.GetFieldSafe<decimal>(_totalGbpName);
        DateTime date = DateTimeOffset.Parse(csv.GetFieldSafe(_timeName), CultureInfo.InvariantCulture).DateTime;

        return new InterestIncome
        {
            AssetName = "Broker interest",
            Amount = new DescribedMoney(totalGbp, WrappedMoney.BaseCurrency, 1, "Interest from cash account"),
            InterestType = InterestType.SAVINGS,
            Date = date
        };
    }

    public bool CheckFileValidity(string data, string contentType)
    {
        if (contentType != "text/csv") return false;

        try
        {
            using var reader = new StringReader(data);
            using var csv = new CsvReader(reader, _config);
            csv.Read();
            csv.ReadHeader();
            string[] headers = csv.HeaderRecord ?? [];

            // Required fields for Trading212 CSV
            string[] requiredFields = { _actionName, _timeName, _tickerName, _quantityName, _totalGbpName };

            if (!requiredFields.All(field => headers.Contains(field))) return false;
            return true;
        }
        catch (InvalidDataException)
        {
            throw; // Re-throw to provide helpful error message
        }
        catch
        {
            return false;
        }
    }
}
