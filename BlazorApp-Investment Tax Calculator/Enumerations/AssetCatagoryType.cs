using System.ComponentModel;

namespace InvestmentTaxCalculator.Enumerations;

public enum AssetCategoryType
{
    [HmrcAssetCategoryType(AssetGroupType.LISTEDSHARES)]
    [Description("Stock")]
    STOCK,
    [HmrcAssetCategoryType(AssetGroupType.OTHERASSETS)]
    [Description("Future contract")]
    FUTURE,
    [HmrcAssetCategoryType(AssetGroupType.OTHERASSETS)]
    [Description("Foreign currency")]
    FX,
    [HmrcAssetCategoryType(AssetGroupType.OTHERASSETS)]
    [Description("Option")]
    OPTION
}

public enum AssetGroupType
{
    ALL,
    LISTEDSHARES,
    OTHERASSETS,
}

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class HmrcAssetCategoryTypeAttribute(AssetGroupType assetGroupType) : Attribute
{
    public AssetGroupType AssetGroupType { get; } = assetGroupType;
}