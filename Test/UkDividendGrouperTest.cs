using CapitalGainCalculator.Model;
using CapitalGainCalculator.Model.UkTaxModel;
using CapitalGainCalculator.Parser.InteractiveBrokersXml;
using NodaMoney;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CapitalGainCalculator.Test
{
    public class UkDividendGrouperTest
    {
        private readonly UkDividendAnalyser _ukDividendGrouper = new UkDividendAnalyser();

        [Fact]
        public void TestDividendGrouping()
        {
            Dividend dividend1 = new Dividend
            {
                AssetName = "abc",
                CompanyLocation = new RegionInfo("HK"),
                DividendType = Enum.DividendType.DIVIDEND,
                Date = new DateTime(2022, 4, 5),
                Proceed = new DescribedMoney { Amount = new Money(1000, "HKD"), Description = "abc dividend", FxRate = 0.11m }
            };
            Dividend dividend2 = new Dividend
            {
                AssetName = "HSBC bank",
                CompanyLocation = new RegionInfo("HK"),
                DividendType = Enum.DividendType.DIVIDEND_IN_LIEU,
                Date = new DateTime(2022, 4, 5),
                Proceed = new DescribedMoney { Amount = new Money(500, "HKD"), Description = "HSBC dividend", FxRate = 0.11m }
            };
            Dividend dividend3 = new Dividend
            {
                AssetName = "def",
                CompanyLocation = new RegionInfo("GB"),
                DividendType = Enum.DividendType.DIVIDEND,
                Date = new DateTime(2022, 4, 4),
                Proceed = new DescribedMoney { Amount = new Money(2000, "GBP"), Description = "def dividend", FxRate = 1m }
            };
            Dividend dividend4 = new Dividend
            {
                AssetName = "def",
                CompanyLocation = new RegionInfo("GB"),
                DividendType = Enum.DividendType.WITHHOLDING,
                Date = new DateTime(2022, 4, 4),
                Proceed = new DescribedMoney { Amount = new Money(100, "GBP"), Description = "def withholding tax", FxRate = 1m }
            };
            Dividend dividend5 = new Dividend
            {
                AssetName = "def",
                CompanyLocation = new RegionInfo("JP"),
                DividendType = Enum.DividendType.DIVIDEND,
                Date = new DateTime(2022, 4, 6),
                Proceed = new DescribedMoney { Amount = new Money(20000, "JPY"), Description = "def dividend", FxRate = 0.0063m }
            };
            Dividend dividend6 = new Dividend
            {
                AssetName = "def",
                CompanyLocation = new RegionInfo("JP"),
                DividendType = Enum.DividendType.WITHHOLDING,
                Date = new DateTime(2022, 4, 6),
                Proceed = new DescribedMoney { Amount = new Money(3000, "JPY"), Description = "def withholding tax", FxRate = 0.0063m }
            };
            IList<TaxEvent> data = new List<TaxEvent>{dividend1, dividend2, dividend3, dividend4, dividend5,dividend6};
            string result = _ukDividendGrouper.AnalyseTaxEventsData(data);
            result.Replace("\r\n", "\n").ShouldBe(
                """
                Tax Year: 2021
                	Region: Hong Kong SAR
                		Total dividends: £165.00
                		Total withholding tax: £0.00

                		Transactions:
                		Asset Name: abc, Date: 05/04/2022, Type: Dividend, Amount: HK$1,000.00, FxRate: 0.11, Sterling Amount: £110.00, Description: abc dividend
                		Asset Name: HSBC bank, Date: 05/04/2022, Type: Payment In Lieu of a Dividend, Amount: HK$500.00, FxRate: 0.11, Sterling Amount: £55.00, Description: HSBC dividend

                	Region: United Kingdom
                		Total dividends: £2000.00
                		Total withholding tax: £100.00

                		Transactions:
                		Asset Name: def, Date: 04/04/2022, Type: Dividend, Amount: £2,000.00, FxRate: 1, Sterling Amount: £2000.00, Description: def dividend
                		Asset Name: def, Date: 04/04/2022, Type: Withholding Tax, Amount: £100.00, FxRate: 1, Sterling Amount: £100.00, Description: def withholding tax


                Tax Year: 2022
                	Region: Japan
                		Total dividends: £126.00
                		Total withholding tax: £18.90

                		Transactions:
                		Asset Name: def, Date: 06/04/2022, Type: Dividend, Amount: ¥20,000, FxRate: 0.0063, Sterling Amount: £126.00, Description: def dividend
                		Asset Name: def, Date: 06/04/2022, Type: Withholding Tax, Amount: ¥3,000, FxRate: 0.0063, Sterling Amount: £18.90, Description: def withholding tax



                """.Replace("\r\n", "\n"));
        }
    }
}
