using InvestmentTaxCalculator.Model.Interfaces;

namespace InvestmentTaxCalculator.Model.UkTaxModel;

public class UkDividendCalculator(IDividendLists dividendList, ITaxYear taxYear) : IDividendCalculator
{
    public List<DividendSummary> CalculateTax()
    {
        List<DividendSummary> result = [];
        var GroupedDividends = from dividend in dividendList.Dividends
                               let taxYear = taxYear.ToTaxYear(dividend.Date)
                               group dividend by new { taxYear, dividend.CompanyLocation };
        foreach (var group in GroupedDividends)
        {
            DividendSummary dividendSummary = new()
            {
                CountryOfOrigin = group.Key.CompanyLocation,
                TaxYear = group.Key.taxYear,
                RelatedDividendsAndTaxes = [.. group]
            };
            result.Add(dividendSummary);
        }
        return result;
    }
}
