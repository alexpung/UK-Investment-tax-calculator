using Enum;

using Model;
using Model.TaxEvents;

using System.Xml.Linq;

namespace Parser.InteractiveBrokersXml;

public static class IBXmlFxParser
{
    public static IList<Trade> ParseXml(XElement document)
    {
        IEnumerable<XElement> filteredElements = document.Descendants("StatementOfFundsLine")
            .Where(trade => !trade.GetAttribute("currency").Equals(WrappedMoney.BaseCurrency, StringComparison.OrdinalIgnoreCase))
            .Where(trade => !trade.GetAttribute("activityDescription").Contains("System Transfer"));
        return filteredElements.Select(trade => TradeMaker(trade, document)).Where(trade => trade != null).ToList<Trade?>()!;
    }

    private static FxTrade? TradeMaker(XElement element, XElement document)
    {
        try
        {
            decimal amountOfFx = Math.Abs(decimal.Parse(element.GetAttribute("amount")));
            string currency = element.GetAttribute("currency");
            DateTime transactionDate = DateTime.Parse(element.GetAttribute("date"));
            DescribedMoney valueInSterlingWrapped = new() { Amount = new WrappedMoney(amountOfFx, currency), FxRate = FetchFxRate(document, currency, WrappedMoney.BaseCurrency, transactionDate) };
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

    private static decimal FetchFxRate(XElement tree, string currency, string baseCurrency, DateTime date)
    {
        // fx rate is 1 if currency is the same as base currency, no need to look up
        if (currency == baseCurrency)
        {
            return 1m;
        }

        XElement? fxRateNode = tree.Descendants("ConversionRate")?.FirstOrDefault(element => DateTime.Parse(element.GetAttribute("reportDate")) == date &&
                                                         element.GetAttribute("fromCurrency").Equals(currency, StringComparison.OrdinalIgnoreCase) &&
                                                         element.GetAttribute("toCurrency").Equals(baseCurrency, StringComparison.OrdinalIgnoreCase));

        if (fxRateNode == null)
        {
            throw new ArgumentException($"No fx rate found for {currency} against {baseCurrency} on {date}");
        }

        decimal result = Convert.ToDecimal(fxRateNode.Attribute("rate")?.Value);

        if (result == -1)
        {
            throw new ArgumentException($"fx rate is -1 for {currency} against {baseCurrency} on {date}, this is an error rate");
        }
        return result;
    }
}
