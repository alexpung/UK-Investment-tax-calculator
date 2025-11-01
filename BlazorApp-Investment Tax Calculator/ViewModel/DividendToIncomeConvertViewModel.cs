using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.ViewModel;

public class DividendToIncomeConvertViewModel(TaxEventLists taxEventLists)
{
    public IEnumerable<Dividend> SelectableTickers =>
        taxEventLists.Dividends
            .DistinctBy(d => d.AssetName)
            .OrderBy(d => d.AssetName);

    /// <summary>
    /// Convert all dividends of the given ticker to income events.
    /// </summary>
    /// <param name="ticker"></param>
    public void ConvertDividendsToIncome(HashSet<string> tickers)
    {
        List<Dividend> dividendsToConvert = [.. taxEventLists.Dividends.Where(dividend => tickers.Contains(dividend.AssetName))];
        foreach (Dividend dividend in dividendsToConvert)
        {
            InterestIncome interestIncome = new()
            {
                AssetName = dividend.AssetName,
                Date = dividend.Date,
                InterestType = InterestType.ETFDIVIDEND,
                IncomeLocation = dividend.CompanyLocation,
                Amount = dividend.Proceed,
            };
            taxEventLists.InterestIncomes.Add(interestIncome);
            taxEventLists.Dividends.Remove(dividend);
        }
    }
}
