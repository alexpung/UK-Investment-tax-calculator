﻿using InvestmentTaxCalculator.Model.Interfaces;

namespace InvestmentTaxCalculator.Model.UkTaxModel;
public class UkSection104Pools(ITaxYear taxYearModel)
{
    private readonly Dictionary<string, UkSection104> _section104Pools = [];

    public List<UkSection104> GetSection104s()
    {
        return [.. _section104Pools.Values];
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
