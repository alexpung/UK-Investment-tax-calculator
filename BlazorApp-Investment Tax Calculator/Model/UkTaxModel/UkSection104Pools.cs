using InvestmentTaxCalculator.Model.Interfaces;

namespace InvestmentTaxCalculator.Model.UkTaxModel;
public class UkSection104Pools(ITaxYear taxYearModel)
{
    private readonly Dictionary<string, UkSection104> _section104Pools = [];

    /// <summary>
    /// Retrieves all UkSection104 instances currently stored in the pool.
    /// </summary>
    /// <returns>A list containing every UkSection104 instance managed by the pool.</returns>
    public List<UkSection104> GetSection104s()
    {
        return [.. _section104Pools.Values];
    }

    /// <summary>
    /// Return the status of all section104s at the end of the tax year that are not empty
    /// </summary>
    /// <param name="taxYear"></param>
    /// <summary>
    /// Retrieves the end-of-year Section104 history entries with non-zero quantity for the specified tax year.
    /// </summary>
    /// <param name="taxYear">The tax year used to determine the end-of-year state of each Section104.</param>
    /// <returns>
    /// A dictionary mapping asset names to their respective Section104History entries for the tax year's end, including only those entries where the new quantity is non-zero.
    /// </returns>
    public Dictionary<string, Section104History> GetEndOfYearSection104s(int taxYear)
    {
        Dictionary<string, Section104History> endOfYearSection104s = new();
        foreach (var section104 in _section104Pools)
        {
            Section104History? lastHistory = section104.Value.GetLastSection104History(taxYearModel.GetTaxYearEndDate(taxYear));
            if (lastHistory is not null && lastHistory.NewQuantity != 0)
            {
                endOfYearSection104s[section104.Key] = lastHistory;
            }
        }
        return endOfYearSection104s;
    }

    /// <summary>
    /// Return all section104s that have history in the given tax year
    /// </summary>
    /// <param name="taxYear"></param>
    /// <returns></returns>
    public List<UkSection104> GetActiveSection104s(int taxYear)
    {
        return [.. _section104Pools.Values.Where(section104 => section104.Section104HistoryList.Exists(history => taxYearModel.ToTaxYear(history.Date) == taxYear))];
    }

    /// <summary>
    /// Return all section104s that have history in the given tax years
    /// </summary>
    /// <param name="taxYear"></param>
    /// <returns></returns>
    public List<UkSection104> GetActiveSection104s(IEnumerable<int> taxYear)
    {
        return [.. _section104Pools.Values.Where(section104 => section104.Section104HistoryList.Exists(history => taxYear.Contains(taxYearModel.ToTaxYear(history.Date))))];
    }

    /// <summary>
    /// If the section104 pool does not exist, create a new one and return the initialised pool, otherwise return the existing pool
    /// </summary>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public virtual UkSection104 GetExistingOrInitialise(string assetName)
    {
        _section104Pools.TryGetValue(assetName, out UkSection104? section104);
        if (section104 is null)
        {
            section104 = new(assetName);
            _section104Pools[assetName] = section104;
        }
        return section104;
    }

    public virtual void Clear()
    {
        _section104Pools.Clear();
    }
}
