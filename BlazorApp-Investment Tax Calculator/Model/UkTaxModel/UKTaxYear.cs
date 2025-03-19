using InvestmentTaxCalculator.Model.Interfaces;

namespace InvestmentTaxCalculator.Model.UkTaxModel;

public class UKTaxYear : ITaxYear
{
    /// <summary>
    /// Determines the UK tax year for a given date.
    /// </summary>
    /// <param name="dateTime">The date to evaluate for tax year determination.</param>
    /// <returns>
    /// The tax year as an integer. Dates in January through March or in early April (before the 6th)
    /// are assigned to the previous year; all other dates are assigned to the current year.
    /// </returns>
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

    /// <summary>
    /// Returns the start date of the specified UK tax year.
    /// </summary>
    /// <param name="taxYear">The tax year for which to retrieve the start date.</param>
    /// <returns>A DateOnly object representing April 6 of the specified tax year.</returns>
    public DateOnly GetTaxYearStartDate(int taxYear)
    {
        return new DateOnly(taxYear, 4, 6);
    }

    /// <summary>
    /// Returns the end date of the specified UK tax year.
    /// </summary>
    /// <param name="taxYear">
    /// The tax year for which to determine the end date. The end date corresponds to April 5 of the following year.
    /// </param>
    /// <returns>
    /// A DateOnly representing April 5 of the year following the provided tax year.
    /// </returns>
    public DateOnly GetTaxYearEndDate(int taxYear)
    {
        return new DateOnly(taxYear + 1, 4, 5);
    }
}
