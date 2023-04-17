using System.Collections.Generic;

namespace CapitalGainCalculator.Model.UkTaxModel;
public class UkSection104Pools
{
    private readonly Dictionary<string, UkSection104> _section104Pools = new();

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
}
