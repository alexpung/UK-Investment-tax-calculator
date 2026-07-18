using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvestmentTaxCalculator.Parser.Json;

public class JsonParseController(AssetTypeToLoadSetting assetTypeToLoadSetting, ResidencyStatusRecord residencyStatusRecord) : ITaxEventFileParser
{
    public TaxEventLists ParseFile(string data)
    {
        TaxEventLists resultFiltered = new();
        ExportImportData? result = JsonSerializer.Deserialize(data, MyJsonContext.Default.ExportImportData);
        if (result == null) return resultFiltered;
        if (result.ResidencyStatusRanges is { Count: > 0 })
        {
            residencyStatusRecord.Ranges = result.ResidencyStatusRanges;
        }
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
[JsonSerializable(typeof(ExportImportData))]
[JsonSerializable(typeof(ExcessReportableIncome))]
[JsonSerializable(typeof(FundEqualisation))]
internal partial class MyJsonContext : JsonSerializerContext { }
