using Enum;

using Model;
using Model.TaxEvents;

using System.Globalization;
using System.Xml.Linq;

namespace Parser.InteractiveBrokersXml;

public class IBXmlFxParser
{
    private readonly Dictionary<ExchangeRateKey, decimal> _exchangeRateCache = [];
    public IList<Trade> ParseXml(XElement document)
    {
        CacheExchangeRates(document);
        IEnumerable<XElement> filteredElements = document.Descendants("StatementOfFundsLine")
            .Where(trade => !trade.GetAttribute("currency").Equals(WrappedMoney.BaseCurrency, StringComparison.OrdinalIgnoreCase))
            .Where(trade => !trade.GetAttribute("activityDescription").Contains("System Transfer"))
            .Where(trade => trade.GetAttribute("levelOfDetail").Equals("Currency"));
        return filteredElements.Select(TradeMaker).Where(trade => trade != null).ToList<Trade?>()!;
    }

    private sealed record ExchangeRateKey(DateOnly Date, string FromCurrency, string ToCurrency);

    private void CacheExchangeRates(XElement document)
    {
        var exchangeRates = document.Descendants("ConversionRate");
        foreach (XElement exchangeRate in exchangeRates)
        {
            ExchangeRateKey exchangeRateKey = new(
                Date: DateOnly.Parse(exchangeRate.GetAttribute("reportDate"), CultureInfo.InvariantCulture),
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
            if (amountOfFx == 0) return null; // Nothing to tax if amount is 0.
            string currency = element.GetAttribute("currency");
            DateTime reportDate = DateTime.Parse(element.GetAttribute("reportDate"), CultureInfo.InvariantCulture);
            DescribedMoney valueInSterlingWrapped = new()
            {
                Amount = new WrappedMoney(amountOfFx, currency),
                FxRate = FetchFxRate(currency, WrappedMoney.BaseCurrency, reportDate)
            };
            return new FxTrade
            {
                BuySell = GetTradeType(element),
                AssetName = currency,
                AssetType = AssetCatagoryType.FX,
                Description = element.GetAttribute("activityDescription"),
                Date = reportDate,
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
