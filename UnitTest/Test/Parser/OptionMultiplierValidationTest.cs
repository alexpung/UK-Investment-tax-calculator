using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Parser;

namespace UnitTest.Test.Parser;

public class OptionMultiplierValidationTest
{
    [Fact]
    public void CheckOptions_InconsistentMultipliers_ShouldThrowException()
    {
        // Arrange
        var option1 = new OptionTrade
        {
            AssetName = "AAPL230131C00150000",
            Date = DateTime.Now,
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = DateTime.Now.AddDays(10),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 100, // Multiplier 100
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Option 1"
        };

        var option2 = new OptionTrade
        {
            AssetName = "AAPL230131C00150000",
            Date = DateTime.Now,
            Underlying = "AAPL",
            StrikePrice = new WrappedMoney(150),
            ExpiryDate = DateTime.Now.AddDays(10),
            PUTCALL = PUTCALL.CALL,
            Multiplier = 10, // Inconsistent Multiplier 10
            TradeReason = TradeReason.OrderedTrade,
            Quantity = 1,
            GrossProceed = new DescribedMoney(0, "USD", 1),
            Expenses = [],
            AcquisitionDisposal = TradeType.ACQUISITION,
            Description = "Option 2"
        };

        var taxEventLists = new TaxEventLists();
        taxEventLists.OptionTrades.AddRange([option1, option2]);

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => OptionHelper.CheckOptions(taxEventLists));
        exception.Message.ShouldContain("Inconsistent multipliers found for option AAPL230131C00150000");
        exception.Message.ShouldContain("100");
        exception.Message.ShouldContain("10");
    }
}
