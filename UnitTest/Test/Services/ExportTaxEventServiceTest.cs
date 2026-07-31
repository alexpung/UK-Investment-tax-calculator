using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Parser.Json;
using InvestmentTaxCalculator.Services;

using System.Text.Json;

using ResidencyEnum = InvestmentTaxCalculator.Enumerations.ResidencyStatus;

namespace UnitTest.Test.Services;

public class ExportTaxEventServiceTest
{
    private static Trade CreateTrade() => new()
    {
        AssetName = "ABC",
        Date = new DateTime(2024, 5, 1),
        AcquisitionDisposal = TradeType.ACQUISITION,
        Quantity = 100,
        GrossProceed = new DescribedMoney(1000m, "GBP", 1m)
    };

    [Fact]
    public void ExportedFile_RoundTripsTaxEventsAndResidencyStatus()
    {
        TaxEventLists taxEventLists = new();
        taxEventLists.Trades.Add(CreateTrade());
        ResidencyStatusRecord exportResidencyRecord = new();
        exportResidencyRecord.SetResidencyStatus(new DateOnly(2020, 4, 6), new DateOnly(2023, 4, 5), ResidencyEnum.NonResident);
        ShareIdentityRegistry exportShareIdentityRegistry = new();
        exportShareIdentityRegistry.LinkShares("OLDCO", "NEWCO");
        ExportTaxEventService exportService = new(taxEventLists, exportResidencyRecord, exportShareIdentityRegistry);

        string json = exportService.SerialiseTaxEvents();
        json.ShouldNotContain("\"AllEvents\"");

        ResidencyStatusRecord importResidencyRecord = new();
        ShareIdentityRegistry importShareIdentityRegistry = new();
        JsonParseController importController = new(new AssetTypeToLoadSetting(), importResidencyRecord, importShareIdentityRegistry);
        TaxEventLists imported = importController.ParseFile(json);

        imported.Trades.Count.ShouldBe(1);
        imported.Trades[0].AssetName.ShouldBe("ABC");
        importResidencyRecord.Ranges.ShouldBe(exportResidencyRecord.Ranges);
        importResidencyRecord.GetResidencyStatus(new DateOnly(2021, 1, 1)).ShouldBe(ResidencyEnum.NonResident);
        importResidencyRecord.GetResidencyStatus(new DateOnly(2024, 1, 1)).ShouldBe(ResidencyEnum.Resident);
        importShareIdentityRegistry.ManualLinks.ShouldBe(exportShareIdentityRegistry.ManualLinks);
    }

    [Fact]
    public void ImportingOldFormatFile_WithoutResidencyStatus_LeavesResidencyRecordUntouched()
    {
        TaxEventLists taxEventLists = new();
        taxEventLists.Trades.Add(CreateTrade());
        string oldFormatJson = JsonSerializer.Serialize(taxEventLists);

        ResidencyStatusRecord importResidencyRecord = new();
        importResidencyRecord.SetResidencyStatus(new DateOnly(2020, 4, 6), new DateOnly(2023, 4, 5), ResidencyEnum.NonResident);
        List<ResidencyStatusRecord.RangeEntry> rangesBeforeImport = [.. importResidencyRecord.Ranges];
        JsonParseController importController = new(new AssetTypeToLoadSetting(), importResidencyRecord, new ShareIdentityRegistry());

        TaxEventLists imported = importController.ParseFile(oldFormatJson);

        imported.Trades.Count.ShouldBe(1);
        importResidencyRecord.Ranges.ShouldBe(rangesBeforeImport);
    }
}
