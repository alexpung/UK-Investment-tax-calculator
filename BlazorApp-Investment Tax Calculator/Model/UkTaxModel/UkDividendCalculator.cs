using Model.Interfaces;

namespace Model.UkTaxModel;

public class UkDividendCalculator : IDividendCalculator
{
    private readonly IDividendLists _dividendList;
    private readonly ITaxYear _year;

    public UkDividendCalculator(IDividendLists dividendList, ITaxYear taxYear)
    {
        _dividendList = dividendList;
        _year = taxYear;
    }

    public List<DividendSummary> CalculateTax()
    {
        List<DividendSummary> result = new();
        var GroupedDividends = from dividend in _dividendList.Dividends
                               let taxYear = _year.ToTaxYear(dividend.Date)
                               group dividend by new { taxYear, dividend.CompanyLocation };
        foreach (var group in GroupedDividends)
        {
            DividendSummary dividendSummary = new()
            {
                CountryOfOrigin = group.Key.CompanyLocation,
                TaxYear = group.Key.taxYear,
                RelatedDividendsAndTaxes = group.ToList()
            };
            result.Add(dividendSummary);
        }
        return result;
    }
}
