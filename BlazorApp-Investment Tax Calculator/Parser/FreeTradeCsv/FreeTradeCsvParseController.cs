using CsvHelper;
using CsvHelper.Configuration;

using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Globalization;

namespace InvestmentTaxCalculator.Parser.FreeTradeCsv;

public class FreeTradeCsvParseController : ITaxEventFileParser
{

    public TaxEventLists ParseFile(string data)
    {
        var trades = new TaxEventLists();
        using var reader = new StringReader(data);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = args => throw new InvalidDataException($"Field missing at index {args.Index} on row {args.Context.Parser?.Row}")
        };
        using var csv = new CsvReader(reader, config);
        // Read the header first
        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            // Get the row type (Column Index 1: "Type")
            string rowType = csv.GetField("Type");
            switch (rowType)
            {
                case "ORDER":
                    trades.Trades.Add(new Trade
                    {
                        AssetName = csv.GetField("Ticker") ?? throw new InvalidDataException($"Ticker field is missing for ORDER on row {csv.Context.Parser?.Row}."),
                        Quantity = csv.GetField<decimal>("Quantity"),
                        GrossProceed = new DescribedMoney(csv.GetField<decimal>("Total Amount"), WrappedMoney.BaseCurrency, 1),
                        Date = DateTimeOffset.Parse(csv.GetField("Timestamp"), CultureInfo.InvariantCulture).DateTime,
                        AcquisitionDisposal = csv.GetField("Buy / Sell").Equals("BUY", StringComparison.CurrentCultureIgnoreCase)
                               ? TradeType.ACQUISITION
                               : TradeType.DISPOSAL
                    });
                    break;
                case "DIVIDEND":
                    if (!DateOnly.TryParse(csv.GetField("Dividend Pay Date"), CultureInfo.InvariantCulture, out DateOnly dateOnly)) throw new InvalidDataException($"Invalid date format for Dividend Pay Date on row {csv.Context.Parser?.Row}.");
                    DateTime dividendDate = dateOnly.ToDateTime(TimeOnly.MinValue);
                    trades.Dividends.Add(new Dividend
                    {
                        AssetName = csv.GetField("Ticker") ?? throw new InvalidDataException($"Ticker field is missing for DIVIDEND on row {csv.Context.Parser?.Row}."),
                        Proceed = new DescribedMoney(csv.GetField<decimal>("Total Amount"), WrappedMoney.BaseCurrency, 1, $"{csv.GetField("Title")} dividend: {csv.GetField("Dividend Amount Per Share")} per share."),
                        Date = dividendDate,
                        DividendType = DividendType.DIVIDEND,
                        CompanyLocation = CountryCode.GetRegionByTwoDigitCode(csv.GetField("ISIN")[..2])
                    });
                    trades.Dividends.Add(new Dividend
                    {
                        AssetName = csv.GetField("Ticker") ?? throw new InvalidDataException($"Ticker field is missing for DIVIDEND on row {csv.Context.Parser?.Row}."),
                        Proceed = new DescribedMoney(csv.GetField<decimal>("Dividend Withheld Tax Amount"), WrappedMoney.BaseCurrency, 1, $"{csv.GetField("Title")} withholding tax"),
                        Date = dividendDate,
                        DividendType = DividendType.WITHHOLDING,
                        CompanyLocation = CountryCode.GetRegionByTwoDigitCode(csv.GetField("ISIN")[..2])
                    });

                    break;
                case "INTEREST_FROM_CASH":
                    trades.InterestIncomes.Add(new InterestIncome
                    {
                        AssetName = "Broker interest",
                        Amount = new DescribedMoney(csv.GetField<decimal>("Total Amount"), WrappedMoney.BaseCurrency, 1, "Interest from cash account."),
                        InterestType = InterestType.SAVINGS,
                        Date = DateTimeOffset.Parse(csv.GetField("Timestamp"), CultureInfo.InvariantCulture).DateTime,
                    });
                    break;
                default:
                    break;
            }
        }
        return trades;
    }

    public bool CheckFileValidity(string data, string contentType)
    {
        if (contentType != "text/csv") return false;
        using var reader = new StringReader(data);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = args => throw new InvalidDataException($"Field missing at index {args.Index} on row {args.Context.Parser?.Row}")
        };
        using var csv = new CsvReader(reader, config);
        csv.Read();
        csv.ReadHeader();
        string[] headers = csv.HeaderRecord;
        string[] requiredFields = { "Type", "Timestamp", "Ticker", "Quantity", "Total Amount" };
        if (requiredFields.All(field => headers.Contains(field)))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

}
