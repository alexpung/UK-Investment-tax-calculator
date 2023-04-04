﻿namespace CapitalGainCalculator.Model;
public class AssetTypeToLoadSetting
{
    public bool LoadStocks { get; set; } = true;
    public bool LoadOptions { get; set; } = true;
    public bool LoadFutures { get; set; } = true;
    public bool LoadFx { get; set; } = true;
    public bool LoadDividend { get; set; } = true;
}