using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Services;

namespace InvestmentTaxCalculator.Model.UkTaxModel;

public class UkDividendCalculator(IDividendLists dividendList, ITaxYear taxYear, ToastService toastService) : IDividendCalculator
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

        if (dividendList.InterestIncomes.Exists(taxEvent => taxEvent.InterestType is TaxEvents.InterestType.ACCURREDINCOMEPROFIT or TaxEvents.InterestType.ACCURREDINCOMELOSS))
        {
            toastService.ShowWarning("Accrued income profit/loss detected. Please go to dividend/income tab and adjust taxable year and rerun calculation.");
        }

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
