using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

namespace InvestmentTaxCalculator.Model.TaxEvents;

/// <summary>
/// Represents a corporate ticker rename where holdings move from an old ticker to a new ticker
/// without creating an acquisition or disposal.
/// </summary>
public record TickerRenameCorporateAction : CorporateAction, IChangeSection104
{
    public override IReadOnlyList<string> CompanyTickersInProcessingOrder => [AssetName, NewTicker];

    public required string NewTicker { get; init; }

    public override string Reason => $"Ticker rename from {AssetName} to {NewTicker} on {Date:d}";

    public override AssetCategoryType AppliesToAssetCategoryType { get; } = AssetCategoryType.STOCK;

    private decimal _transferQuantity;
    private WrappedMoney _transferCost = WrappedMoney.GetBaseCurrencyZero();

    public override MatchAdjustment TradeMatching(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, MatchAdjustment matchAdjustment)
    {
        return matchAdjustment;
    }

    public override void ChangeSection104(UkSection104 section104)
    {
        if (AssetName == section104.AssetName)
        {
            ProcessOldTicker(section104);
        }
        else if (NewTicker == section104.AssetName)
        {
            ProcessNewTicker(section104);
        }
    }

    private void ProcessOldTicker(UkSection104 section104)
    {
        _transferQuantity = 0m;
        _transferCost = WrappedMoney.GetBaseCurrencyZero();

        decimal oldQuantity = section104.Quantity;
        WrappedMoney oldCost = section104.AcquisitionCostInBaseCurrency;

        if (oldQuantity == 0m)
        {
            return;
        }

        _transferQuantity = oldQuantity;
        _transferCost = oldCost;

        string explanation = $"Ticker renamed to {NewTicker} on {Date:d}. Pool transferred to new ticker.";
        section104.ClearSection104(Date, explanation);
    }

    private void ProcessNewTicker(UkSection104 section104)
    {
        if (_transferQuantity == 0m)
        {
            return;
        }

        string explanation = $"Ticker rename from {AssetName} on {Date:d}: {_transferQuantity:0.####} shares moved with cost basis {_transferCost}.";
        section104.AddAssets(Date, _transferQuantity, _transferCost, null, explanation);
    }

    public override string GetDuplicateSignature()
    {
        return $"TICKERRENAME|{base.GetDuplicateSignature()}|{NewTicker}";
    }
}
