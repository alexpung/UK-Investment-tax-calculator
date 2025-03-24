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
        result = JsonSerializer.Deserialize(data, MyJsonContext.Default.TaxEventLists);
        TaxEventLists resultFiltered = new();
        if (result == null) return resultFiltered;
        resultFiltered = assetTypeToLoadSetting.FilterTaxEvent(result);
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
