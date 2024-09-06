namespace InvestmentTaxCalculator.Model;
public class AssetTypeToLoadSetting
{
    public bool LoadStocks { get; set; } = true;
    public bool LoadOptions { get; set; }
    public bool LoadFutures { get; set; } = true;
    public bool LoadFx { get; set; } = true;
    public bool LoadDividends { get; set; } = true;
}
