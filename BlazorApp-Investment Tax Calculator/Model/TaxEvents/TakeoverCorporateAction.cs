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
    public bool ElectTaxDeferral { get; init; }

    /// <summary>
    /// Market value of the new shares at the event date (needed for proportioned cost calculation in cash scenarios)
    /// </summary>
    public DescribedMoney? NewSharesMarketValue { get; init; }

    /// <summary>
    /// Stores the disposal generated for cash payment (recreated on each calculation run)
    /// </summary>
    [JsonIgnore]
    public CorporateActionTaxCalculation? CashDisposal { get; private set; }

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
            ProcessCashComponent(oldCost);
        }

        // Build explanation including acquisition history
        string explanation = BuildOldCompanyExplanation(section104, oldQuantity, oldCost);

        // Empty the old company S104 pool
        section104.ClearSection104(Date, explanation);
    }

    private void ProcessCashComponent(WrappedMoney oldCost)
    {
        if (CashComponent == null || NewSharesMarketValue == null)
            throw new InvalidOperationException($"Invalid operation in processing cash payout in {AssetName}, both {nameof(CashComponent)} and {nameof(NewSharesMarketValue)} required to calculate taxable gain.");

        WrappedMoney cashAmount = CashComponent.BaseCurrencyAmount;
        bool isSmallCash = IsSmallCash(cashAmount.Amount);
        WrappedMoney allowableCostUsed;

        if (isSmallCash && ElectTaxDeferral)
        {
            // Defer tax by reducing cost basis
            // Allowable Cost for this transaction is the amount by which we reduce the pool
            // If Cash < Cost, we reduce by Cash. Gain = 0.
            // If Cash > Cost, we reduce by full Cost (to 0). Gain = Cash - Cost.
            allowableCostUsed = WrappedMoney.Min(cashAmount, oldCost);

            // Remaining cost is original minus what we used
            _transferCost = oldCost - allowableCostUsed;
            WrappedMoney gainAmount = cashAmount - allowableCostUsed;
            if (gainAmount.Amount > 0)
            {
                string calculationDetail = $"Small cash treatment (deferred): \n" +
                                           $"\tCash Received: {cashAmount}\n" +
                                           $"\tTotal Cost Basis: {oldCost}\n" +
                                           $"\tExcess Gain: {cashAmount} - {oldCost} = {gainAmount}\n" +
                                           $"\tAllowable Cost used to reduce gain: {oldCost}";
                CreateCashDisposal(gainAmount, WrappedMoney.GetBaseCurrencyZero(), calculationDetail);
            }
        }
        else
        {
            // Normal matching (Section 104 part-disposal rules)
            // Calculate proportioned cost
            WrappedMoney totalValue = cashAmount + NewSharesMarketValue.BaseCurrencyAmount;
            decimal cashProportion = cashAmount.Amount / totalValue.Amount;
            allowableCostUsed = oldCost * cashProportion;

            _transferCost = oldCost - allowableCostUsed;

            string calculationDetail = $"Proportioned cost calculation: \n" +
                                       $"\tCash: {cashAmount}\n" +
                                       $"\tNew Shares Value: {NewSharesMarketValue.PrintToTextFile()}\n" +
                                       $"\tTotal Value: {totalValue}\n" +
                                       $"\tCash Proportion: {cashAmount.Amount:F2} / {totalValue.Amount:F2} = {cashProportion:P2}\n" +
                                       $"\tAllowable Cost: {oldCost} * {cashProportion:P2} = {allowableCostUsed}";

            CreateCashDisposal(cashAmount, allowableCostUsed, calculationDetail);
        }
    }

    private bool IsSmallCash(decimal cashAmountInBaseCurrency)
    {
        // Small cash is defined as £3000 or 5% of total market value
        const decimal smallCashThreshold = 3000m;

        if (cashAmountInBaseCurrency <= smallCashThreshold)
            return true;

        // Check 5% rule if we have market value information
        if (NewSharesMarketValue?.Amount.Amount > 0)
        {
            decimal totalMarketValue = cashAmountInBaseCurrency + NewSharesMarketValue.BaseCurrencyAmount.Amount;
            decimal fivePercentOfTotalMarketValue = totalMarketValue * 0.05m;
            return cashAmountInBaseCurrency <= fivePercentOfTotalMarketValue;
        }
        return false;
    }

    public static bool IsSmallCash(decimal cashAmountInBaseCurrency, DescribedMoney NewSharesMarketValue)
    {
        // Small cash is defined as £3000 or 5% of total market value
        const decimal smallCashThreshold = 3000m;

        if (cashAmountInBaseCurrency <= smallCashThreshold)
            return true;

        // Check 5% rule if we have market value information
        if (NewSharesMarketValue?.Amount.Amount > 0)
        {
            decimal totalMarketValue = cashAmountInBaseCurrency + NewSharesMarketValue.BaseCurrencyAmount.Amount;
            decimal fivePercentOfTotalMarketValue = totalMarketValue * 0.05m;
            return cashAmountInBaseCurrency <= fivePercentOfTotalMarketValue;
        }
        return false;
    }

    private void CreateCashDisposal(WrappedMoney proceeds, WrappedMoney allowableCost, string additionalInfo)
    {
        CashDisposal = new CorporateActionTaxCalculation(this, proceeds, allowableCost, additionalInfo);
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
