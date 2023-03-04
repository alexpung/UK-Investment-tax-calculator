using System;

namespace CapitalGainCalculator.Model.UkTaxModel
{
    public static class UKTaxYear
    {
        public static int ToTaxYear(DateTime dateTime)
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
}
