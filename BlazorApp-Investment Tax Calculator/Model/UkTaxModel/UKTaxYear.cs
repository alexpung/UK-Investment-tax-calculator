using InvestmentTaxCalculator.Model.Interfaces;

namespace InvestmentTaxCalculator.Model.UkTaxModel;

public class UKTaxYear : ITaxYear
{
    public int ToTaxYear(DateTime dateTime)
    {
        return (dateTime.Month, dateTime.Day) switch
        {
            ( <= 3, _) => dateTime.Year - 1,
            (4, < 6) => dateTime.Year - 1,
            (4, >= 6) => dateTime.Year,
            ( >= 5, _) => dateTime.Year
        };
    }

    public DateOnly GetTaxYearStartDate(int taxYear)
    {
        return new DateOnly(taxYear, 4, 6);
    }

    public DateOnly GetTaxYearEndDate(int taxYear)
    {
        return new DateOnly(taxYear + 1, 4, 5);
    }
}
