using Model.Interfaces;

namespace Model.UkTaxModel;
public class UkSection104Pools
{
    private readonly Dictionary<string, UkSection104> _section104Pools = new();

    public List<UkSection104> GetSection104s()
    {
        return _section104Pools.Values.ToList();
    }

    /// <summary>
    /// If the section104 pool does not exist, create a new one and return the initialised pool, otherwise return the existing pool
    /// </summary>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public UkSection104 GetExistingOrInitialise(string assetName)
    {
        _section104Pools.TryGetValue(assetName, out UkSection104? section104);
        if (section104 is null)
        {
            section104 = new(assetName);
            _section104Pools[assetName] = section104;
        }
        return section104;
    }

    public void Clear()
    {
        _section104Pools.Clear();
    }

    /// <summary>
    /// Get Section104 history up until the occurrence of the trade calculation group
    /// </summary>
    /// <param name="taxCalculation"></param>
    /// <returns></returns>
    public List<Section104History> GetHistory(ITradeTaxCalculation taxCalculation)
    {
        string assetName = taxCalculation.AssetName;
        List<Section104History> section104Histories = _section104Pools[assetName].Section104HistoryList;
        return section104Histories.TakeWhile(i => i.TradeTaxCalculation != taxCalculation)
                                  .Concat(section104Histories.Where(i => i.TradeTaxCalculation == taxCalculation))
                                  .ToList();
    }
}
