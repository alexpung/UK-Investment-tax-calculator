using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.UnitTest.Test.Model.TaxEvents;

using Shouldly;
using Xunit;

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
        var dateString = new DateTime(2023, 1, 1).ToString("d");
        reason.ShouldContain($"Test Fund fund equalisation of £100.00 on {dateString} (Related Dividend)");
        reason.ShouldEndWith("\n");
        reason.ShouldNotContain("..");
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

        var dateString = new DateTime(2023, 1, 1).ToString("d");
        reason.ShouldContain($"Test Fund fund equalisation of £100.00 on {dateString}");
        reason.ShouldEndWith("\n");
        reason.ShouldNotContain("()");
        reason.ShouldNotContain(" .");
    }
}
