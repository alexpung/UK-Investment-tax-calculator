using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Parser;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Parser.InteractiveBrokersXml;

public class JsonParseController(AssetTypeToLoadSetting assetTypeToLoadSetting) : ITaxEventFileParser
{
    public TaxEventLists ParseFile(string data)
    {
        TaxEventLists? result = null;
        try
        {
            result = JsonSerializer.Deserialize(data, MyJsonContext.Default.TaxEventLists);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        TaxEventLists resultFiltered = new();
        if (result == null) return resultFiltered;
        if (assetTypeToLoadSetting.LoadDividends) resultFiltered.Dividends.AddRange(result.Dividends);
        if (assetTypeToLoadSetting.LoadStocks) resultFiltered.CorporateActions.AddRange(result.CorporateActions);
        if (assetTypeToLoadSetting.LoadStocks) resultFiltered.Trades.AddRange(result.Trades.Where(trade => trade.AssetType == AssetCatagoryType.STOCK));
        if (assetTypeToLoadSetting.LoadFutures) resultFiltered.Trades.AddRange(result.Trades.Where(trade => trade.AssetType == AssetCatagoryType.FUTURE));
        if (assetTypeToLoadSetting.LoadFx) resultFiltered.Trades.AddRange(result.Trades.Where(trade => trade.AssetType == AssetCatagoryType.FX));
        return resultFiltered;
    }

    public bool CheckFileValidity(string data, string contentType)
    {
        return contentType == "application/json";
    }
}


/// <summary>
/// Workaround due to trimmer problem https://github.com/dotnet/runtime/issues/62242
/// </summary>
[JsonSerializable(typeof(TaxEventLists))]
internal partial class MyJsonContext : JsonSerializerContext { }
