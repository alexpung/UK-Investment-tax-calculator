using InvestmentTaxCalculator.Model.Interfaces;

namespace InvestmentTaxCalculator.Model.UkTaxModel;

public class UkDividendCalculator(IDividendLists dividendList, ITaxYear taxYear) : IDividendCalculator
{
    public List<DividendSummary> CalculateTax()
    {
        List<DividendSummary> dividendSummaries = [];
        var groupedDividendsDict = dividendList.Dividends
            .GroupBy(d => new { TaxYear = taxYear.ToTaxYear(d.Date), d.CompanyLocation })
            .ToDictionary(g => (g.Key.TaxYear, g.Key.CompanyLocation), g => g.ToList());

        var groupedInterestIncomesDict = dividendList.InterestIncomes
            .GroupBy(i => (i.YearTaxable, i.IncomeLocation))
            .ToDictionary(g => g.Key, g => g.ToList());

        IEnumerable<(int, CountryCode)> combinedKeys = groupedDividendsDict.Keys.Union(groupedInterestIncomesDict.Keys);
        foreach (var key in combinedKeys)
        {
            groupedDividendsDict.TryGetValue(key, out var dividends);
            groupedInterestIncomesDict.TryGetValue(key, out var interests);
            dividendSummaries.Add(new DividendSummary
            {
                CountryOfOrigin = key.Item2,
                TaxYear = key.Item1,
                RelatedDividendsAndTaxes = dividends ?? [],
                RelatedInterestIncome = interests ?? []
            });
        }
        return dividendSummaries;
    }
}
