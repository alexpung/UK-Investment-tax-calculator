using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Services;
using System.Linq;

namespace InvestmentTaxCalculator.Model.UkTaxModel;

public class UkDividendCalculator(IDividendLists dividendList, ITaxYear taxYear, ToastService toastService, ResidencyStatusRecord residencyStatusRecord) : IDividendCalculator
{
    public List<DividendSummary> CalculateTax()
    {
        _ = toastService;
        List<DividendSummary> dividendSummaries = [];
        var groupedDividendsDict = dividendList.Dividends
            .Where(g => residencyStatusRecord.GetResidencyStatus(DateOnly.FromDateTime(g.Date)) == ResidencyStatus.Resident)
            .GroupBy(d => new { TaxYear = taxYear.ToTaxYear(d.Date), d.CompanyLocation })
            .ToDictionary(g => (g.Key.TaxYear, g.Key.CompanyLocation), g => g.ToList());

        var groupedInterestIncomesDict = dividendList.InterestIncomes
            .Where(g => residencyStatusRecord.GetResidencyStatus(DateOnly.FromDateTime(g.Date)) == ResidencyStatus.Resident)
            .GroupBy(i => (taxYear.ToTaxYear(i.Date) + (i.IsTaxDeferred ? 1:0), i.IncomeLocation))
            .ToDictionary(g => g.Key, g => g.ToList());

        // Logic to determine if the next payment is in the same tax year
        var interestIncomesByAsset = dividendList.InterestIncomes
            .GroupBy(i => i.AssetName)
            .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Date).ToList());

        foreach (var interest in dividendList.InterestIncomes)
        {
            if (interest.InterestType is InterestType.ACCURREDINCOMEPROFIT or InterestType.ACCURREDINCOMELOSS)
            {
                if (interestIncomesByAsset.TryGetValue(interest.AssetName, out List<InterestIncome>? assetEvents))
                {
                    // Find the BOND event with the smallest Date that is strictly greater than the accrued interest event's Date
                    var nextBondPayment = assetEvents
                       .Where(i => i.InterestType == InterestType.BOND && i.Date > interest.Date)
                       .FirstOrDefault(); 

                    if (nextBondPayment != null)
                    {
                        int accruedTaxYear = taxYear.ToTaxYear(interest.Date);
                        int paymentTaxYear = taxYear.ToTaxYear(nextBondPayment.Date);
                        interest.IsNextPaymentInSameTaxYear = (accruedTaxYear == paymentTaxYear);
                    }
                    else
                    {
                        // If there is no bond payment of the same ticker afterwards assume the accurred interest is taxable in the next year
                        interest.IsNextPaymentInSameTaxYear = false;
                    }
                }
            }
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
