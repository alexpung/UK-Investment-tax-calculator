using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Services;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvestmentTaxCalculator.Parser.Json;

public class JsonParseController(AssetTypeToLoadSetting assetTypeToLoadSetting, ResidencyStatusRecord residencyStatusRecord,
    ShareIdentityRegistry shareIdentityRegistry) : ITaxEventFileParser
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
        if (result.ShareIdentityLinks is { Count: > 0 })
        {
            // Restore user declared share links so they apply when the imported events are registered.
            shareIdentityRegistry.ImportManualLinks(result.ShareIdentityLinks);
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
[JsonSerializable(typeof(ShareIdentityLink))]
internal partial class MyJsonContext : JsonSerializerContext { }
