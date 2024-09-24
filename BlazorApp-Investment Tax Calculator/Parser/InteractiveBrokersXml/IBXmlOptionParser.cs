﻿using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Globalization;
using System.Xml.Linq;

namespace InvestmentTaxCalculator.Parser.InteractiveBrokersXml;

public static class IBXmlOptionTradeParser
{
    public static IList<OptionTrade> ParseXml(XElement document)
    {
        IEnumerable<XElement> filteredElements = document.Descendants("Order").Where(row => row.GetAttribute("levelOfDetail") == "ORDER" &&
                                                                                                             row.GetAttribute("assetCategory") == "OPT");
        return filteredElements.Select(element => XmlParserHelper.ParserExceptionManager(OptionTradeMaker, element))
                                                                                          .Where(trade => trade != null).ToList()!;

    }

    private static OptionTrade? OptionTradeMaker(XElement element)
    {
        return new OptionTrade
        {
            AcquisitionDisposal = element.GetTradeType(),
            AssetName = element.GetAttribute("symbol"),
            Description = element.GetAttribute("description"),
            Date = DateTime.Parse(element.GetAttribute("dateTime"), CultureInfo.InvariantCulture),
            Quantity = element.GetQuantity(),
            GrossProceed = element.GetGrossProceed(),
            Expenses = element.BuildExpenses(),
            Underlying = element.GetAttribute("underlyingSymbol"),
            StrikePrice = element.BuildMoney("strike", "currency"),
            ExpiryDate = DateTime.Parse(element.GetAttribute("expiry"), CultureInfo.InvariantCulture),
            Multiplier = decimal.Parse(element.GetAttribute("multiplier")),
            PUTCALL = element.GetAttribute("putCall") switch
            {
                "C" => PUTCALL.CALL,
                "P" => PUTCALL.PUT,
                _ => throw new ParseException($"Unknown putCall {element.GetAttribute("putCall")} for {element}")
            },
            TradeReason = element.GetAttribute("notes") switch
            {
                string s when s.Split(";").Contains("Ex") => TradeReason.OwnerExerciseOption,
                string s when s.Split(";").Contains("A") => TradeReason.OptionAssigned,
                string s when s.Split(";").Contains("Ep") => TradeReason.Expired,
                _ => TradeReason.OrderedTrade
            }
        };
    }
}

