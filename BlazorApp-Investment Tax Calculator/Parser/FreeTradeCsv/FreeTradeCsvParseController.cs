using CsvHelper;
using CsvHelper.Configuration;

using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Globalization;

namespace InvestmentTaxCalculator.Parser.FreeTradeCsv;

public class FreeTradeCsvParseController : ITaxEventFileParser
{
    private const string _totalAmountName = "Total Amount";
    private const string _tickerName = "Ticker";
    private const string _isinName = "ISIN";
    private const string _titleName = "Title";
    private const string _typeName = "Type";
    private const string _timeStampName = "Timestamp";
    private const string _quantityName = "Quantity";

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
            string rowType = csv.GetFieldSafe(_typeName);
            switch (rowType)
            {
                case "ORDER":
                    trades.Trades.Add(new Trade
                    {
                        AssetName = csv.GetFieldSafe(_tickerName),
                        Quantity = csv.GetFieldSafe<decimal>(_quantityName),
                        GrossProceed = new DescribedMoney(csv.GetField<decimal>(_totalAmountName), WrappedMoney.BaseCurrency, 1),
                        Date = DateTimeOffset.Parse(csv.GetFieldSafe(_timeStampName), CultureInfo.InvariantCulture).DateTime,
                        AcquisitionDisposal = csv.GetFieldSafe("Buy / Sell").Equals("BUY", StringComparison.CurrentCultureIgnoreCase)
                               ? TradeType.ACQUISITION
                               : TradeType.DISPOSAL
                    });
                    break;
                case "DIVIDEND":
                    DateTime dividendDate = csv.ParseDateStringToDateTime("Dividend Pay Date");
                    trades.Dividends.Add(new Dividend
                    {
                        AssetName = csv.GetFieldSafe(_tickerName),
                        Proceed = new DescribedMoney(csv.GetField<decimal>(_totalAmountName), WrappedMoney.BaseCurrency, 1, $"{csv.GetField(_titleName)} dividend: {csv.GetField("Dividend Amount Per Share")} per share."),
                        Date = dividendDate,
                        DividendType = DividendType.DIVIDEND,
                        CompanyLocation = CountryCode.GetRegionByTwoDigitCode(csv.GetFieldSafe(_isinName)[..2])
                    });
                    trades.Dividends.Add(new Dividend
                    {
                        AssetName = csv.GetFieldSafe(_tickerName),
                        Proceed = new DescribedMoney(csv.GetField<decimal>("Dividend Withheld Tax Amount"), WrappedMoney.BaseCurrency, 1, $"{csv.GetField(_titleName)} withholding tax"),
                        Date = dividendDate,
                        DividendType = DividendType.WITHHOLDING,
                        CompanyLocation = CountryCode.GetRegionByTwoDigitCode(csv.GetFieldSafe(_isinName)[..2])
                    });

                    break;
                case "INTEREST_FROM_CASH":
                    trades.InterestIncomes.Add(new InterestIncome
                    {
                        AssetName = "Broker interest",
                        Amount = new DescribedMoney(csv.GetFieldSafe<decimal>(_totalAmountName), WrappedMoney.BaseCurrency, 1, "Interest from cash account."),
                        InterestType = InterestType.SAVINGS,
                        Date = DateTimeOffset.Parse(csv.GetFieldSafe(_timeStampName), CultureInfo.InvariantCulture).DateTime,
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
        using var csv = new CsvReader(reader, _config);
        csv.Read();
        csv.ReadHeader();
        string[] headers = csv.HeaderRecord ?? [];
        string[] requiredFields = { _typeName, _timeStampName, _tickerName, _quantityName, _totalAmountName };
        return requiredFields.All(field => headers.Contains(field));
    }
}
