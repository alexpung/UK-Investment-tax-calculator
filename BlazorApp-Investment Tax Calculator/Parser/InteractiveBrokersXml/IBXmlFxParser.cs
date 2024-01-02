using Enum;

using Model;
using Model.TaxEvents;

using System.Xml.Linq;

namespace Parser.InteractiveBrokersXml;

public class IBXmlFxParser
{
    private readonly Dictionary<ExchangeRateKey, decimal> _exchangeRateCache = new();
    public IList<Trade> ParseXml(XElement document)
    {
        CacheExchangeRates(document);
        IEnumerable<XElement> filteredElements = document.Descendants("StatementOfFundsLine")
            .Where(trade => !trade.GetAttribute("currency").Equals(WrappedMoney.BaseCurrency, StringComparison.OrdinalIgnoreCase))
            .Where(trade => !trade.GetAttribute("activityDescription").Contains("System Transfer"));
        return filteredElements.Select(TradeMaker).Where(trade => trade != null).ToList<Trade?>()!;
    }

    private sealed record ExchangeRateKey(DateOnly Date, string FromCurrency, string ToCurrency);

    private void CacheExchangeRates(XElement document)
    {
        var exchangeRates = document.Descendants("ConversionRate");
        foreach (XElement exchangeRate in exchangeRates)
        {
            ExchangeRateKey exchangeRateKey = new(
                Date: DateOnly.Parse(exchangeRate.GetAttribute("reportDate")),
                FromCurrency: exchangeRate.GetAttribute("fromCurrency").ToLower(),
                ToCurrency: exchangeRate.GetAttribute("toCurrency").ToLower()
            );
            _exchangeRateCache[exchangeRateKey] = decimal.Parse(exchangeRate.GetAttribute("rate"));
        }
    }

    private FxTrade? TradeMaker(XElement element)
    {
        try
        {
            decimal amountOfFx = Math.Abs(decimal.Parse(element.GetAttribute("amount")));
            string currency = element.GetAttribute("currency");
            DateTime transactionDate = DateTime.Parse(element.GetAttribute("date"));
            DescribedMoney valueInSterlingWrapped = new()
            {
                Amount = new WrappedMoney(amountOfFx, currency),
                FxRate = FetchFxRate(currency, WrappedMoney.BaseCurrency, transactionDate)
            };
            return new FxTrade
            {
                BuySell = GetTradeType(element),
                AssetName = currency,
                AssetType = AssetCatagoryType.FX,
                Description = element.GetAttribute("activityDescription"),
                Date = transactionDate,
                Quantity = amountOfFx,
                GrossProceed = valueInSterlingWrapped
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return null;
        }
    }

    private static TradeType GetTradeType(XElement element) => decimal.Parse(element.GetAttribute("amount")) switch
    {
        >= 0 => TradeType.BUY,
        < 0 => TradeType.SELL,
    };

    private decimal FetchFxRate(string currency, string baseCurrency, DateTime date)
    {
        // fx rate is 1 if currency is the same as base currency, no need to look up
        if (currency == baseCurrency)
        {
            return 1m;
        }
        try
        {
            ExchangeRateKey exchangeRateKey = new(DateOnly.FromDateTime(date), currency.ToLower(), baseCurrency.ToLower());
            decimal result = _exchangeRateCache[exchangeRateKey];
            if (result == -1)
            {
                throw new ArgumentException($"fx rate is -1 for {currency} against {baseCurrency} on {date}, this is an error rate");
            }
            return result;
        }
        catch (KeyNotFoundException)
        {
            throw new ArgumentException($"No fx rate found for {currency} against {baseCurrency} on {date}");
        }
    }
}
