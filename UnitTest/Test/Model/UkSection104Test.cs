using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using UnitTest.Helper;

namespace UnitTest.Test.Model;

public class UkSection104Test
{
    [Theory]
    [InlineData(100, 1000, 50, 400)]
    [InlineData(100, 1000, 100, 5000)]
    [InlineData(100, 1000, 150, 4000)]
    public void TestAddandRemoveSection104(decimal buyQuantity, decimal buyValue, decimal sellQuantity, decimal sellValue)
    {
        TradeTaxCalculation buyTradeTaxCalculation = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Local), buyQuantity, buyValue, TradeType.ACQUISITION);
        UkSection104 ukSection104 = new("IBM");
        buyTradeTaxCalculation.MatchWithSection104(ukSection104);
        ukSection104.AssetName.ShouldBe("IBM");
        ukSection104.Quantity.ShouldBe(100m);
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(buyValue));
        buyTradeTaxCalculation.MatchHistory[0].MatchAcquisitionQty.ShouldBe(buyQuantity);
        buyTradeTaxCalculation.MatchHistory[0].BaseCurrencyMatchDisposalProceed.ShouldBe(WrappedMoney.GetBaseCurrencyZero());
        buyTradeTaxCalculation.MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SECTION_104);
        TradeTaxCalculation sellTradeTaxCalculation = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Local), sellQuantity, sellValue, TradeType.DISPOSAL);
        sellTradeTaxCalculation.MatchWithSection104(ukSection104);
        ukSection104.Quantity.ShouldBe(decimal.Max(buyQuantity - sellQuantity, 0));
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(decimal.Max((buyQuantity - sellQuantity) / buyQuantity * buyValue, 0)));
        sellTradeTaxCalculation.MatchHistory[0].MatchAcquisitionQty.ShouldBe(decimal.Min(sellQuantity, buyQuantity));
        sellTradeTaxCalculation.MatchHistory[0].BaseCurrencyMatchAllowableCost.ShouldBe(new WrappedMoney(decimal.Min(buyValue / buyQuantity * sellQuantity, buyValue)));
        sellTradeTaxCalculation.MatchHistory[0].BaseCurrencyMatchDisposalProceed.ShouldBe(new WrappedMoney(decimal.Min(sellQuantity, buyQuantity) * sellValue / sellQuantity));
        sellTradeTaxCalculation.MatchHistory[0].TradeMatchType.ShouldBe(TaxMatchType.SECTION_104);
    }

    [Fact]
    public void TestSection104History()
    {
        TradeTaxCalculation tradeTaxCalculation1 = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Local), 100, 1000m, TradeType.ACQUISITION);
        TradeTaxCalculation tradeTaxCalculation2 = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 3, 2, 0, 0, 0, DateTimeKind.Local), 200, 2000m, TradeType.ACQUISITION);
        TradeTaxCalculation tradeTaxCalculation3 = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 5, 3, 0, 0, 0, DateTimeKind.Local), 300, 3000m, TradeType.ACQUISITION);
        TradeTaxCalculation tradeTaxCalculation4 = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 7, 4, 0, 0, 0, DateTimeKind.Local), 400, 8000m, TradeType.DISPOSAL);
        UkSection104 ukSection104 = new("IBM");
        tradeTaxCalculation1.MatchWithSection104(ukSection104);
        tradeTaxCalculation2.MatchWithSection104(ukSection104);
        tradeTaxCalculation3.MatchWithSection104(ukSection104);
        tradeTaxCalculation4.MatchWithSection104(ukSection104);
        ukSection104.Section104HistoryList[0].OldQuantity.ShouldBe(0);
        ukSection104.Section104HistoryList[0].OldValue.ShouldBe(new WrappedMoney(0m));
        ukSection104.Section104HistoryList[0].QuantityChange.ShouldBe(100);
        ukSection104.Section104HistoryList[0].ValueChange.ShouldBe(new WrappedMoney(1000m));
        ukSection104.Section104HistoryList[1].OldQuantity.ShouldBe(100);
        ukSection104.Section104HistoryList[1].OldValue.ShouldBe(new WrappedMoney(1000m));
        ukSection104.Section104HistoryList[1].QuantityChange.ShouldBe(200);
        ukSection104.Section104HistoryList[1].ValueChange.ShouldBe(new WrappedMoney(2000m));
        ukSection104.Section104HistoryList[2].OldQuantity.ShouldBe(300);
        ukSection104.Section104HistoryList[2].OldValue.ShouldBe(new WrappedMoney(3000m));
        ukSection104.Section104HistoryList[2].QuantityChange.ShouldBe(300);
        ukSection104.Section104HistoryList[2].ValueChange.ShouldBe(new WrappedMoney(3000m));
        ukSection104.Section104HistoryList[3].OldQuantity.ShouldBe(600);
        ukSection104.Section104HistoryList[3].OldValue.ShouldBe(new WrappedMoney(6000m));
        ukSection104.Section104HistoryList[3].QuantityChange.ShouldBe(-400);
        ukSection104.Section104HistoryList[3].ValueChange.ShouldBe(new WrappedMoney(-4000m));
    }

    [Fact]
    public void TestSection104HandleShareSplit()
    {
        TradeTaxCalculation tradeTaxCalculation1 = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Local), 100, 1000m, TradeType.ACQUISITION);
        TradeTaxCalculation tradeTaxCalculation2 = MockTrade.CreateTradeTaxCalculation("IBM", new DateTime(2020, 3, 2, 0, 0, 0, DateTimeKind.Local), 120, 1500m, TradeType.DISPOSAL);
        StockSplit corporateAction = new() { AssetName = "IBM", Date = new DateTime(2020, 3, 1, 0, 0, 0, DateTimeKind.Local), SplitTo = 3, SplitFrom = 2 };
        // Also test wrong AssetName don't change S104
        StockSplit corporateAction2 = new() { AssetName = "ABC", Date = new DateTime(2020, 3, 1, 0, 0, 0, DateTimeKind.Local), SplitTo = 3, SplitFrom = 1 };
        UkSection104 ukSection104 = new("IBM");
        tradeTaxCalculation1.MatchWithSection104(ukSection104);
        corporateAction.ChangeSection104(ukSection104);
        corporateAction2.ChangeSection104(ukSection104);
        tradeTaxCalculation2.MatchWithSection104(ukSection104);
        ukSection104.Quantity.ShouldBe(30); // bought 100, 150 after split - 120 sold = 30
        ukSection104.AcquisitionCostInBaseCurrency.ShouldBe(new WrappedMoney(200m)); // bought shares worth 1000, remaining shares worth = 30*1000/150 = 200
    }

    [Fact]
    public void TestGetLastSection104History_OnLastReportingDate()
    {
        // 1. Arrange: Buy shares on the last reporting date
        DateTime reportingDate = new(2023, 12, 31, 10, 0, 0, DateTimeKind.Local);
        TradeTaxCalculation tradeAtReportingDate = MockTrade.CreateTradeTaxCalculation("ETF1", reportingDate, 100m, 1000m, TradeType.ACQUISITION);
        UkSection104 ukSection104 = new("ETF1");
        tradeAtReportingDate.MatchWithSection104(ukSection104);

        // 2. Act: Simulate ERI logic (getting quantity at reporting date)
        var history = ukSection104.GetLastSection104History(DateOnly.FromDateTime(reportingDate));

        // 3. Assert: Verify the acquisition on the reporting date is included
        history.ShouldNotBeNull();
        history.NewQuantity.ShouldBe(100m);
        history.Date.ShouldBe(reportingDate);
    }
    [Fact]
    public void TestSection104AcquisitionExplanationWithExpenses()
    {
        // Arrange
        var assetName = "EXPN";
        var date = new DateTime(2023, 1, 1);
        var quantity = 100m;
        var grossProceedAmount = 1000m;
        var expenseAmount = 5m;

        var trade = new Trade
        {
            AssetName = assetName,
            Date = date,
            AcquisitionDisposal = TradeType.ACQUISITION,
            Quantity = quantity,
            GrossProceed = new DescribedMoney { Amount = new WrappedMoney(grossProceedAmount), FxRate = 1 },
            Expenses = [new DescribedMoney { Amount = new WrappedMoney(expenseAmount), FxRate = 1, Description = "Commission" }]
        };

        var tradeTaxCalculation = new TradeTaxCalculation([trade]);
        var ukSection104 = new UkSection104(assetName);

        // Act
        tradeTaxCalculation.MatchWithSection104(ukSection104);

        // Assert
        var history = ukSection104.Section104HistoryList[0];
        history.Explanation.ShouldNotContain("Total base cost");
        history.Explanation.ShouldNotContain("Total expenses");
        history.Explanation.ShouldNotContain("Trade 1: ");
        history.Explanation.ShouldContain("Base cost: £1,000.00");
        history.Explanation.ShouldContain("Commission: £5.00");
    }

    [Fact]
    public void TestSection104MultipleAcquisitionsExplanation()
    {
        // Arrange
        var assetName = "MULTI";
        var date = new DateTime(2023, 1, 1);
        
        var trade1 = new Trade
        {
            AssetName = assetName,
            Date = date,
            AcquisitionDisposal = TradeType.ACQUISITION,
            Quantity = 100m,
            GrossProceed = new DescribedMoney { Amount = new WrappedMoney(1000m), FxRate = 1 },
            Expenses = [new DescribedMoney { Amount = new WrappedMoney(1m), FxRate = 1, Description = "Fee1" }]
        };
        
        var trade2 = new Trade
        {
            AssetName = assetName,
            Date = date,
            AcquisitionDisposal = TradeType.ACQUISITION,
            Quantity = 200m,
            GrossProceed = new DescribedMoney { Amount = new WrappedMoney(2000m), FxRate = 1 },
            Expenses = [new DescribedMoney { Amount = new WrappedMoney(2m), FxRate = 1, Description = "Fee2" }]
        };

        var tradeTaxCalculation = new TradeTaxCalculation([trade1, trade2]);
        var ukSection104 = new UkSection104(assetName);

        // Act
        tradeTaxCalculation.MatchWithSection104(ukSection104);

        // Assert
        var history = ukSection104.Section104HistoryList[0];
        history.Explanation.ShouldContain("Total proportioned base cost: £3,000.00");
        history.Explanation.ShouldContain("Total proportioned expenses: £3.00");
        history.Explanation.ShouldContain("Trade 1: Base cost: £1,000.00");
        history.Explanation.ShouldContain("Fee1: £1.00");
        history.Explanation.ShouldContain("Trade 2: Base cost: £2,000.00");
        history.Explanation.ShouldContain("Fee2: £2.00");
    }

    [Fact]
    public void TestSection104ProportionedAcquisitionExplanation()
    {
        // Arrange
        var assetName = "PROP";
        var date = new DateTime(2023, 1, 1);
        var totalQuantity = 200m;
        var matchedQuantity = 100m; // Match only half
        var grossProceedAmount = 2000m;
        var expenseAmount = 20m;

        var trade = new Trade
        {
            AssetName = assetName,
            Date = date,
            AcquisitionDisposal = TradeType.ACQUISITION,
            Quantity = totalQuantity,
            GrossProceed = new DescribedMoney { Amount = new WrappedMoney(grossProceedAmount), FxRate = 1 },
            Expenses = [new DescribedMoney { Amount = new WrappedMoney(expenseAmount), FxRate = 1, Description = "Tax" }]
        };

        var tradeTaxCalculation = new TradeTaxCalculation([trade]);
        var ukSection104 = new UkSection104(assetName);

        // Act
        // Manually match only half the quantity with Section 104
        ukSection104.AddAssets(tradeTaxCalculation, matchedQuantity, tradeTaxCalculation.TotalCostOrProceed * (matchedQuantity / totalQuantity));

        // Assert
        var history = ukSection104.Section104HistoryList[0];
        // Proportioned base cost: 2000 * (100/200) = 1000
        // Proportioned expense: 20 * (100/200) = 10
        history.Explanation.ShouldNotContain("Total proportioned base cost"); // Only 1 trade
        history.Explanation.ShouldContain("Base cost: £1,000.00");
        history.Explanation.ShouldContain("Tax: £10.00");
    }

    [Fact]
    public void TestSection104FutureAcquisitionExplanation()
    {
        // Arrange
        var assetName = "FUTURE";
        var date = new DateTime(2023, 1, 1);
        var quantity = 1m;
        var contractValueAmount = 50000m;
        var currency = "Usd";
        var fxRate = 0.8m; // Usd to Gbp

        var futureTrade = new FutureContractTrade
        {
            AssetName = assetName,
            Date = date,
            AcquisitionDisposal = TradeType.ACQUISITION,
            Quantity = quantity,
            GrossProceed = new DescribedMoney { Amount = new WrappedMoney(0, currency), FxRate = fxRate },
            ContractValue = new DescribedMoney { Amount = new WrappedMoney(contractValueAmount, currency), FxRate = fxRate },
            PositionType = PositionType.OPENLONG
        };

        var tradeTaxCalculation = new TradeTaxCalculation([futureTrade]);
        var ukSection104 = new UkSection104(assetName);
        var proportionedContractValue = new WrappedMoney(contractValueAmount, currency);

        // Act
        ukSection104.AddAssets(tradeTaxCalculation, quantity, WrappedMoney.GetBaseCurrencyZero(), proportionedContractValue);

        // Assert
        var history = ukSection104.Section104HistoryList[0];
        history.Explanation.ShouldContain("Contract Value: $50,000.00");
    }
}
