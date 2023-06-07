using Model.Interfaces;

namespace Model.UkTaxModel;

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
}
