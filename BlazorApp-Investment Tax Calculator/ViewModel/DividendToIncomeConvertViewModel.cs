using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

namespace InvestmentTaxCalculator.ViewModel;

/// <summary>A ticker whose dividends can be converted to taxable income, with what it paid.</summary>
/// <param name="IsFund">True when the ticker's trades are funds (ETF / mutual fund), where bond funds are found.</param>
/// <param name="Payments">Number of dividend payments, not counting withholding tax entries.</param>
public record DividendTickerSummary(
    string Ticker, bool IsFund, int Payments, WrappedMoney TotalDividend, DateTime FirstPayment, DateTime LastPayment);

/// <summary>A ticker whose dividends have already been converted to "ETF dividend income".</summary>
public record ConvertedTickerSummary(string Ticker, int Payments, WrappedMoney TotalIncome);

public class DividendToIncomeConvertViewModel(TaxEventLists taxEventLists)
{
    public IEnumerable<Dividend> SelectableTickers =>
        taxEventLists.Dividends
            .DistinctBy(d => d.CanonicalAssetName)
            .OrderBy(d => d.CanonicalAssetName);

    /// <summary>
    /// One row per ticker that still has dividends, ordered by ticker.
    /// </summary>
    public IReadOnlyList<DividendTickerSummary> GetTickerSummaries()
    {
        HashSet<string> fundTickers = [.. taxEventLists.Trades
            .Where(trade => trade.AssetType == AssetCategoryType.FUND)
            .Select(trade => trade.CanonicalAssetName)];

        return [.. taxEventLists.Dividends
            .GroupBy(dividend => dividend.CanonicalAssetName)
            .Select(group => new DividendTickerSummary(
                group.Key,
                fundTickers.Contains(group.Key),
                group.Count(dividend => dividend.DividendType != DividendType.WITHHOLDING),
                group.Sum(dividend => dividend.DividendReceived),
                group.Min(dividend => dividend.Date),
                group.Max(dividend => dividend.Date)))
            .OrderBy(summary => summary.Ticker, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Tickers whose dividends have been converted to "ETF dividend income", ordered by ticker.
    /// </summary>
    public IReadOnlyList<ConvertedTickerSummary> GetConvertedSummaries() =>
        [.. taxEventLists.InterestIncomes
            .Where(income => income.InterestType == InterestType.ETFDIVIDEND)
            .GroupBy(income => income.CanonicalAssetName)
            .Select(group => new ConvertedTickerSummary(group.Key, group.Count(), group.Sum(income => income.Amount.BaseCurrencyAmount)))
            .OrderBy(summary => summary.Ticker, StringComparer.OrdinalIgnoreCase)];

    /// <summary>
    /// Convert all dividends of the given ticker to income events.
    /// </summary>
    /// <param name="ticker"></param>
    public void ConvertDividendsToIncome(HashSet<string> tickers)
    {
        List<Dividend> dividendsToConvert = [.. taxEventLists.Dividends.Where(dividend => tickers.Contains(dividend.CanonicalAssetName))];
        foreach (Dividend dividend in dividendsToConvert)
        {
            InterestIncome interestIncome = new()
            {
                AssetName = dividend.AssetName,
                Date = dividend.Date,
                InterestType = InterestType.ETFDIVIDEND,
                IncomeLocation = dividend.CompanyLocation,
                Amount = dividend.Proceed,
                // The converted income is the same asset as the dividend it replaces: carry the ISIN and the
                // resolved share identity across, otherwise CanonicalAssetName falls back to the raw ticker and
                // the income stops grouping with the share it belongs to (e.g. income booked under a former
                // ticker no longer matching that share's trades and dividends).
                Isin = dividend.Isin,
                ShareIdentity = dividend.ShareIdentity,
            };
            taxEventLists.InterestIncomes.Add(interestIncome);
            taxEventLists.Dividends.Remove(dividend);
        }
    }
}
