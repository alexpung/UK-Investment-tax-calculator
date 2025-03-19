namespace InvestmentTaxCalculator.Model.Interfaces;

public interface ITaxYear
{
    public int ToTaxYear(DateTime dateTime);
    public DateOnly GetTaxYearStartDate(int taxYear);
    public DateOnly GetTaxYearEndDate(int taxYear);
}