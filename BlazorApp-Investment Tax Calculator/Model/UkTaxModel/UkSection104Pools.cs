namespace InvestmentTaxCalculator.Model.UkTaxModel;
public class UkSection104Pools
{
    private readonly Dictionary<string, UkSection104> _section104Pools = [];

    public List<UkSection104> GetSection104s()
    {
        return [.. _section104Pools.Values];
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
