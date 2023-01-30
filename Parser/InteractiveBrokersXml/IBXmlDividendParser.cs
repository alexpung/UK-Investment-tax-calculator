using CapitalGainCalculator.Enum;
using CapitalGainCalculator.Model;
using DiffEngine;
using NodaMoney;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace CapitalGainCalculator.Parser.InteractiveBrokersXml
{                 
                
    public class IBXmlDividendParser
    {
        public IEnumerable<Dividend> ParseXml(XElement document)
        {
            IEnumerable<XElement> filteredElements = document.Descendants("CashTransaction").Where(row => GetDividendType(row) != DividendType.NOT_DIVIDEND);
            return filteredElements.Select(DividendMaker).Where(dividend => dividend != null)!;
        }

        private Dividend? DividendMaker(XElement element)
        {
            try
            {
                return new Dividend
                {
                    DividendType = GetDividendType(element),
                    AssetName = element.GetAttribute("symbol"),
                    Date = DateTime.Parse(element.GetAttribute("settleDate")),
                    CompanyLocation = GetCompanyLocation(element),
                    Proceed = element.BuildDescribedMoney("amount", "currency", "fxRateToBase", element.GetAttribute("description"))
                };
            }
             catch { return null; } // TODO Implement suitable catch clause and logging */
        }

        private RegionInfo GetCompanyLocation(XElement dividendElement)
        {
            try
            {
                return new RegionInfo(dividendElement.GetAttribute("isin")[..2]);
            }
            catch(ArgumentException) //CUSIP is shown
            {
                if (dividendElement.GetAttribute("description").Contains("US TAX"))
                {
                    return new RegionInfo("US");
                }
                else if (dividendElement.GetAttribute("description").Contains("CA TAX"))
                {
                    return new RegionInfo("CA");
                }
                else throw new ArgumentException($"Unable to determine Company Location with {dividendElement}");
            }
        }

        private DividendType GetDividendType(XElement dividendElement) => dividendElement.GetAttribute("type") switch
        {
            "Withholding Tax" => DividendType.WITHHOLDING,
            "Dividends" => DividendType.DIVIDEND,
            "Payment In Lieu Of Dividends" => DividendType.DIVIDEND_IN_LIEU,
            _ => DividendType.NOT_DIVIDEND
        };
    }
}
