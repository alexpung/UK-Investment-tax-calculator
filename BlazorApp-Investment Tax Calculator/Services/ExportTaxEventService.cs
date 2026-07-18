using InvestmentTaxCalculator.Model;

using System.Text.Json;

namespace InvestmentTaxCalculator.Services;

public class ExportTaxEventService(TaxEventLists taxEventLists, ResidencyStatusRecord residencyStatusRecord)
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    public string SerialiseTaxEvents()
    {
        ExportImportData exportData = new()
        {
            Trades = taxEventLists.Trades,
            CorporateActions = taxEventLists.CorporateActions,
            Dividends = taxEventLists.Dividends,
            OptionTrades = taxEventLists.OptionTrades,
            FutureContractTrades = taxEventLists.FutureContractTrades,
            CashSettlements = taxEventLists.CashSettlements,
            InterestIncomes = taxEventLists.InterestIncomes,
            ResidencyStatusRanges = residencyStatusRecord.Ranges
        };
        return JsonSerializer.Serialize(exportData, _options);
    }
}
