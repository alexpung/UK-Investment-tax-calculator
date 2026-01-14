using System;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using Xunit;

namespace InvestmentTaxCalculator.UnitTest.Test.Model.TaxEvents;

public class FundEqualisationTest
{
    [Fact]
    public void Reason_WithRelatedEvent_FormatsCorrectly()
    {
        var equalisation = new FundEqualisation
        {
            AssetName = "Test Fund",
            Date = new DateTime(2023, 1, 1),
            Amount = new DescribedMoney(100m, "GBP", 1m, "Equalisation"),
            RelatedEventDescription = "Related Dividend"
        };

        var reason = equalisation.Reason;

        // WrappedMoney uses NMoneys which formats GBP with £ symbol and 2 decimals by default.
        Assert.Contains("Test Fund fund equalisation of £100.00 on 01/01/2023 (Related Dividend)", reason);
        Assert.EndsWith("\n", reason);
        Assert.DoesNotContain("..", reason); 
    }

    [Fact]
    public void Reason_WithoutRelatedEvent_FormatsCorrectly()
    {
        var equalisation = new FundEqualisation
        {
            AssetName = "Test Fund",
            Date = new DateTime(2023, 1, 1),
            Amount = new DescribedMoney(100m, "GBP", 1m, "Equalisation"),
            RelatedEventDescription = null
        };

        var reason = equalisation.Reason;

        Assert.Contains("Test Fund fund equalisation of £100.00 on 01/01/2023", reason);
        Assert.EndsWith("\n", reason);
        Assert.DoesNotContain("()", reason); 
        Assert.DoesNotContain(" .", reason); 
    }
}
