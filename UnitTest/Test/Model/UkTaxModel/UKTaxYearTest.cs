using Model.UkTaxModel;

namespace UnitTest.Test.Model.UkTaxModel;

public class UKTaxYearTests
{
    [Theory]
    [InlineData("2022-01-01", 2021)]
    [InlineData("2022-04-30", 2022)]
    [InlineData("2022-05-01", 2022)]
    [InlineData("2023-04-05", 2022)]
    [InlineData("2023-04-06", 2023)]
    [InlineData("2023-12-31", 2023)]
    public void ToTaxYear_ReturnsCorrectTaxYear(string dateString, int expectedTaxYear)
    {
        // Arrange
        var taxYear = new UKTaxYear();
        DateTime date = DateTime.Parse(dateString);

        // Act
        int result = taxYear.ToTaxYear(date);

        // Assert
        result.ShouldBe(expectedTaxYear);
    }
}
