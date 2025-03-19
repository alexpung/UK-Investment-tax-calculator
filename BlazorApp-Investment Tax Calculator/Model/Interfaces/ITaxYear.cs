namespace InvestmentTaxCalculator.Model.Interfaces;

public interface ITaxYear
{
    /// <summary>
/// Determines the tax year corresponding to a specified date.
/// </summary>
/// <param name="dateTime">The date for which to calculate the tax year.</param>
/// <returns>An integer representing the tax year associated with the provided date.</returns>
public int ToTaxYear(DateTime dateTime);
    /// <summary>
/// Retrieves the start date for the specified tax year.
/// </summary>
/// <param name="taxYear">The tax year for which to determine the start date.</param>
/// <returns>A DateOnly object representing the first day of the provided tax year.</returns>
public DateOnly GetTaxYearStartDate(int taxYear);
    /// <summary>
/// Retrieves the end date for the specified tax year.
/// </summary>
/// <param name="taxYear">An integer representing the tax year.</param>
/// <returns>A <see cref="DateOnly"/> corresponding to the last day of the tax year.</returns>
public DateOnly GetTaxYearEndDate(int taxYear);
}