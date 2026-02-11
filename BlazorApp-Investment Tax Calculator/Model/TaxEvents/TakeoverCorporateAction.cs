using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using System.Text.Json.Serialization;

namespace InvestmentTaxCalculator.Model.TaxEvents;

/// <summary>
/// Represents a takeover or merger where shares of a non-surviving company are exchanged
/// for shares of an acquiring company, with optional cash payment.
/// </summary>
public record TakeoverCorporateAction : CorporateAction, IChangeSection104
{
    public override IReadOnlyList<string> CompanyTickersInProcessingOrder => [AssetName, AcquiringCompanyTicker];

    /// <summary>
    /// Ticker symbol of the acquiring company
    /// </summary>
    public required string AcquiringCompanyTicker { get; init; }

    /// <summary>
    /// Ratio of how many new company shares are received per old company share
    /// For example, if 2 new shares are received for every 1 old share, this value is 2.0
    /// </summary>
    public required decimal OldToNewRatio { get; init; }

    /// <summary>
    /// Optional cash component received in the takeover (null for shares-only takeovers)
    /// </summary>
    public DescribedMoney? CashComponent { get; init; }

    /// <summary>
    /// Whether the user elects to defer tax for small cash component (only applicable if cash is small)
    /// </summary>

    /// <summary>
    /// Market value of the new shares at the event date (needed for proportioned cost calculation in cash scenarios)
    /// </summary>
    public DescribedMoney? NewSharesMarketValue { get; init; }

    public override AssetCategoryType AppliesToAssetCategoryType { get; } = AssetCategoryType.STOCK;

    // Internal state to transfer between old and new company S104 pools
    private decimal _transferQuantity;
    private WrappedMoney _transferCost = WrappedMoney.GetBaseCurrencyZero();

    public override string Reason => CashComponent != null
        ? $"{AssetName} takeover by {AcquiringCompanyTicker} ({OldToNewRatio:F4}:1 ratio) with cash {CashComponent.BaseCurrencyAmount} on {Date:d}"
        : $"{AssetName} takeover by {AcquiringCompanyTicker} ({OldToNewRatio:F4}:1 ratio) on {Date:d}";

    public override MatchAdjustment TradeMatching(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, MatchAdjustment matchAdjustment)
    {
        // Takeover doesn't affect trade matching between other trades
        return matchAdjustment;
    }

    public override void ChangeSection104(UkSection104 section104)
    {
        // Phase 1: Process the old company (non-surviving ticker)
        if (AssetName == section104.AssetName)
        {
            ProcessOldCompany(section104);
        }

        // Phase 2: Process the new company (acquiring ticker)
        else if (AcquiringCompanyTicker == section104.AssetName)
        {
            ProcessNewCompany(section104);
        }
    }

    private void ProcessOldCompany(UkSection104 section104)
    {
        // Reset transfer state to prevent bleeding between conflicting calculator runs (e.g. Option vs Stock)
        _transferQuantity = 0;
        _transferCost = WrappedMoney.GetBaseCurrencyZero();

        // Capture the current S104 pool state
        decimal oldQuantity = section104.Quantity;
        WrappedMoney oldCost = section104.AcquisitionCostInBaseCurrency;

        if (oldQuantity == 0)
        {
            // Nothing to transfer
            return;
        }

        // Calculate initial transfer values
        _transferQuantity = oldQuantity * OldToNewRatio;
        _transferCost = oldCost;

        // Handle cash component if present
        if (CashComponent != null)
        {
            ProcessCashComponent(oldCost, section104);
        }

        // Build explanation including acquisition history
        string explanation = BuildOldCompanyExplanation(section104, oldQuantity, oldCost);

        // Empty the old company S104 pool
        section104.ClearSection104(Date, explanation);
    }

    private void ProcessCashComponent(WrappedMoney oldCost, UkSection104 section104)
    {
        if (CashComponent == null || NewSharesMarketValue == null)
            throw new InvalidOperationException($"Invalid operation in processing cash payout in {AssetName}, both {nameof(CashComponent)} and {nameof(NewSharesMarketValue)} required to calculate taxable gain.");

        WrappedMoney cashAmount = CashComponent.BaseCurrencyAmount;
        WrappedMoney totalValue = cashAmount + NewSharesMarketValue.BaseCurrencyAmount;

        WrappedMoney allowableCostUsed = ProcessCashResult(cashAmount, oldCost, totalValue.Amount, 1.0m, AssetName, section104);
        _transferCost = oldCost - allowableCostUsed;
    }


    private string BuildOldCompanyExplanation(UkSection104 section104, decimal quantity, WrappedMoney cost)
    {
        var explanation = new System.Text.StringBuilder();
        explanation.AppendLine($"Takeover by {AcquiringCompanyTicker} on {Date:d}");
        explanation.AppendLine($"{quantity} shares of {AssetName} exchanged for {_transferQuantity} shares of {AcquiringCompanyTicker}");

        if (CashComponent != null)
        {
            explanation.AppendLine($"Cash received: {CashComponent.BaseCurrencyAmount}");
        }

        explanation.AppendLine($"Total cost transferred: {_transferCost}");
        return explanation.ToString().TrimEnd();
    }

    private void ProcessNewCompany(UkSection104 section104)
    {
        if (_transferQuantity == 0)
        {
            // Nothing to transfer
            return;
        }

        string explanation = $"Takeover of {AssetName} on {Date:d}: {_transferQuantity} shares received with cost basis of {_transferCost}";

        section104.AddAssets(Date, _transferQuantity, _transferCost, null, explanation);

        // Update the explanation in the last history entry
        if (section104.Section104HistoryList.Count > 0)
        {
            var lastHistory = section104.Section104HistoryList[^1];
            lastHistory.Explanation = explanation;
        }
    }

    public override string GetDuplicateSignature()
    {
        string cashInfo = CashComponent != null
            ? $"|{CashComponent.Amount.Amount}|{CashComponent.Amount.Currency}"
            : "|NOCASH";
        return $"TAKEOVER|{base.GetDuplicateSignature()}|{AcquiringCompanyTicker}|{OldToNewRatio}{cashInfo}";
    }
}
