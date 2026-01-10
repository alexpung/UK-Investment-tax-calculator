using CsvHelper.Configuration;

using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.Parser.FreeTradeCsv;

public sealed class FreeTradeTradeMapper : ClassMap<Trade>
{
    public FreeTradeTradeMapper()
    {
        Map(m => m.AssetName).Name("Ticker");
        Map(m => m.Quantity).Name("Quantity");
        Map(m => m.Description).Name("Title");
        Map(m => m.GrossProceed).Convert(args =>
        {
            var totalAmount = args.Row.GetField("Total Amount");
            if (string.IsNullOrWhiteSpace(totalAmount))
            {
                throw new ArgumentException($"Total Amount field is empty or whitespace in {args}.");
            }
            return new DescribedMoney(decimal.Parse(totalAmount), WrappedMoney.BaseCurrency, 1);
        });

        // Map Timestamp to Date
        Map(m => m.Date).Name("Timestamp");

        // Custom Logic for the TradeType Enum (Buy/Sell)
        Map(m => m.AcquisitionDisposal).Convert(args =>
        {
            var rawType = args.Row.GetField("Buy / Sell")?.ToUpper();
            return rawType == "BUY" ? TradeType.ACQUISITION : TradeType.DISPOSAL;
        });
    }
}
