using CapitalGainCalculator.Enum;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CapitalGainCalculator.Model.UkTaxModel
{
    public class UkDividendAnalyser : ITaxAnalyser
    {
        private IEnumerable<IGrouping<int, IGrouping<RegionInfo, Dividend>>> GroupDividend(IEnumerable<TaxEvent> events)
        {
            IEnumerable<Dividend> dividends = from taxEvent in events
                                              where taxEvent is Dividend
                                              select (Dividend)taxEvent;

            var GroupedDividends = from dividend in dividends
                                   let taxYear = UKTaxYear.ToTaxYear(dividend.Date)
                                   group dividend by taxYear into taxYearGroup
                                   from LocationGroup in (
                                        from dividend in taxYearGroup
                                        group dividend by dividend.CompanyLocation
                                        )
                                   group LocationGroup by taxYearGroup.Key;


            return GroupedDividends;
        }

        private string DividendTypeConverter(DividendType dividendType) => dividendType switch
        {
            DividendType.WITHHOLDING => "Withholding Tax",
            DividendType.DIVIDEND_IN_LIEU => "Payment In Lieu of a Dividend",
            DividendType.DIVIDEND => "Dividend",
            _ => throw new NotImplementedException() //SHould not get a dividend object with any other type.
        };

        private decimal SumDividendTotals(IEnumerable<Dividend> dividends)
        {
            return (from dividend in dividends
                    where dividend.DividendType is DividendType.DIVIDEND_IN_LIEU or DividendType.DIVIDEND
                    select dividend.Proceed.BaseCurrencyAmount).Sum();
        }

        private decimal SumWithholdingTotals(IEnumerable<Dividend> dividends)
        {
            return (from dividend in dividends
                    where dividend.DividendType is DividendType.WITHHOLDING
                    select dividend.Proceed.BaseCurrencyAmount).Sum();
        }

        private string PrettyPrintDividend(Dividend dividend)
        {
            return $"Asset Name: {dividend.AssetName}, " +
                    $"Date: {dividend.Date.ToShortDateString()}, " +
                    $"Type: {DividendTypeConverter(dividend.DividendType)}, " +
                    $"Amount: {dividend.Proceed.Amount}, " +
                    $"FxRate: {dividend.Proceed.FxRate}, " +
                    $"Sterling Amount: £{dividend.Proceed.BaseCurrencyAmount:0.00}, " +
                    $"Description: {dividend.Proceed.Description}";
        }

        public string AnalyseTaxEventsData(IEnumerable<TaxEvent> events)
        {
            StringBuilder output = new();
            var result = GroupDividend(events);
            foreach (var taxYear in result)
            {
                output.AppendLine($"Tax Year: {taxYear.Key}");
                foreach (var companyLocation in taxYear)
                {
                    output.AppendLine($"\tRegion: {companyLocation.Key.EnglishName}");
                    output.AppendLine($"\t\tTotal dividends: £{SumDividendTotals(companyLocation):0.00}");
                    output.AppendLine($"\t\tTotal withholding tax: £{SumWithholdingTotals(companyLocation):0.00}\n");
                    output.AppendLine("\t\tTransactions:");
                    foreach (var dividend in companyLocation)
                    {
                        output.AppendLine($"\t\t{PrettyPrintDividend(dividend)}");
                    }
                    output.AppendLine();
                }
                output.AppendLine();
            }
            return output.ToString();
        }
    }
}
