namespace CapitalGainCalculator.Model;
public class AssetTypeToLoadSetting
{
    public bool LoadStocks { get; set; } = true;
    public bool LoadOptions { get; set; }
    public bool LoadFutures { get; set; }
    public bool LoadFx { get; set; }
    public bool LoadDividends { get; set; } = true;
}
