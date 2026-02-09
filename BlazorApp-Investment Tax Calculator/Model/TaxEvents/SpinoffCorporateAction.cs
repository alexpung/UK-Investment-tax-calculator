using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using System.Text.Json.Serialization;

namespace InvestmentTaxCalculator.Model.TaxEvents;

/// <summary>
/// Represents a spinoff corporate action where a parent company distributes shares 
/// of a subsidiary to existing shareholders, requiring cost basis allocation between
/// the parent and the new spinoff company.
/// </summary>
public record SpinoffCorporateAction : CorporateAction, IChangeSection104
{
    /// <summary>
    /// Ticker symbol of the spinoff company (new subsidiary)
    /// </summary>
    public required string SpinoffCompanyTicker { get; init; }

    /// <summary>
    /// Number of spinoff shares received per parent share.
    /// For example, if 0.5 spinoff shares are received for every 1 parent share, this value is 0.5
    /// </summary>
    public required decimal SpinoffSharesPerParentShare { get; init; }

    /// <summary>
    /// Market value of parent shares at the spinoff date (needed for cost allocation calculation)
    /// </summary>
    public required DescribedMoney ParentMarketValue { get; init; }

    /// <summary>
    /// Market value of spinoff shares at the spinoff date (needed for cost allocation calculation)
    /// </summary>
    public required DescribedMoney SpinoffMarketValue { get; init; }

    /// <summary>
    /// Optional cash received in lieu of fractional shares
    /// </summary>
    public DescribedMoney? CashInLieu { get; init; }

    /// <summary>
    /// If true and cash-in-lieu is "small" (under £3,000 or 5% of total value),
    /// elect to defer capital gains by reducing cost basis instead of recognizing a gain.
    /// This follows TCGA 1992 s122.
    /// </summary>
    public bool ElectTaxDeferral { get; init; } = true;

    /// <summary>
    /// Stores the disposal generated for cash-in-lieu payment (recreated on each calculation run)
    /// </summary>
    [JsonIgnore]
    public CorporateActionTaxCalculation? CashDisposal { get; private set; }

    public override AssetCategoryType AppliesToAssetCategoryType { get; } = AssetCategoryType.STOCK;

    // Internal state to transfer between parent and spinoff S104 pools
    private decimal _transferQuantity;
    private WrappedMoney _transferCost = WrappedMoney.GetBaseCurrencyZero();

    /// <summary>
    /// Calculates the percentage of cost basis retained by the parent company
    /// based on market values at the spinoff date.
    /// </summary>
    public decimal ParentRetainedPercentage
    {
        get
        {
            decimal parentValue = ParentMarketValue.BaseCurrencyAmount.Amount;
            decimal spinoffValue = SpinoffMarketValue.BaseCurrencyAmount.Amount;
            decimal totalValue = parentValue + spinoffValue;
            
            if (totalValue == 0) return 1m; // Fallback: keep all cost in parent
            
            return parentValue / totalValue;
        }
    }

    public override string Reason => CashInLieu != null
        ? $"{AssetName} spinoff of {SpinoffCompanyTicker} ({SpinoffSharesPerParentShare:F4}:1 ratio) with cash-in-lieu {CashInLieu.BaseCurrencyAmount} on {Date:d}"
        : $"{AssetName} spinoff of {SpinoffCompanyTicker} ({SpinoffSharesPerParentShare:F4}:1 ratio) on {Date:d}";

    public override MatchAdjustment TradeMatching(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, MatchAdjustment matchAdjustment)
    {
        // Spinoff doesn't affect trade matching between other trades
        return matchAdjustment;
    }

    public override void ChangeSection104(UkSection104 section104)
    {
        // Phase 1: Process the parent company (reduce cost basis)
        if (AssetName == section104.AssetName)
        {
            ProcessParentCompany(section104);
        }

        // Phase 2: Process the spinoff company (add new shares with transferred cost)
        else if (SpinoffCompanyTicker == section104.AssetName)
        {
            ProcessSpinoffCompany(section104);
        }
    }

    private void ProcessParentCompany(UkSection104 section104)
    {
        // Reset transfer state to prevent bleeding between conflicting calculator runs
        _transferQuantity = 0;
        _transferCost = WrappedMoney.GetBaseCurrencyZero();

        // Capture the current S104 pool state
        decimal parentQuantity = section104.Quantity;
        WrappedMoney parentCost = section104.AcquisitionCostInBaseCurrency;

        if (parentQuantity == 0)
        {
            // Nothing to process
            return;
        }

        // Calculate spinoff share quantity
        decimal rawSpinoffQuantity = parentQuantity * SpinoffSharesPerParentShare;
        
        // Only round down to whole shares if cash-in-lieu is provided
        // Some brokers support fractional shares, so shares-only spinoffs keep the exact ratio
        // Round to 4 decimal places to match typical broker precision
        _transferQuantity = CashInLieu != null 
            ? Math.Floor(rawSpinoffQuantity) 
            : Math.Round(rawSpinoffQuantity, 4, MidpointRounding.ToZero);

        // Calculate cost allocation based on market values
        decimal spinoffPercentage = 1m - ParentRetainedPercentage;
        _transferCost = parentCost * spinoffPercentage;

        // Handle cash-in-lieu if present
        WrappedMoney cashCostUsed = WrappedMoney.GetBaseCurrencyZero();
        if (CashInLieu != null)
        {
            cashCostUsed = ProcessCashInLieu(parentCost, section104);
        }

        // Calculate the cost reduction (spinoff allocation minus any cash cost already handled)
        WrappedMoney costReduction = _transferCost + cashCostUsed;

        // Build explanation including the allocation calculation
        string explanation = BuildParentExplanation(section104, parentQuantity, parentCost, parentCost - costReduction);

        // Reduce the parent company S104 pool cost (quantity stays the same)
        // Use negative adjustment to reduce cost
        section104.AdjustAcquisitionCost(costReduction * -1, Date, explanation);
    }

    private WrappedMoney GetCashInLieuCostUsed(WrappedMoney totalCost)
    {
        if (CashInLieu == null) return WrappedMoney.GetBaseCurrencyZero();

        // Cash-in-lieu is treated as a part-disposal
        // The cost attributable to cash is: (Cash / (Cash + SpinoffValue)) * SpinoffAllocationCost
        decimal spinoffPercentage = 1m - ParentRetainedPercentage;
        WrappedMoney spinoffAllocation = totalCost * spinoffPercentage;
        
        WrappedMoney cashAmount = CashInLieu.BaseCurrencyAmount;
        WrappedMoney spinoffShareValue = SpinoffMarketValue.BaseCurrencyAmount;
        
        if (spinoffShareValue.Amount + cashAmount.Amount == 0) return WrappedMoney.GetBaseCurrencyZero();
        
        decimal cashProportion = cashAmount.Amount / (spinoffShareValue.Amount + cashAmount.Amount);
        return spinoffAllocation * cashProportion;
    }

    private WrappedMoney ProcessCashInLieu(WrappedMoney parentCost, UkSection104 section104)
    {
        if (CashInLieu == null) return WrappedMoney.GetBaseCurrencyZero();

        WrappedMoney cashAmount = CashInLieu.BaseCurrencyAmount;
        bool isSmallCash = IsSmallCash(cashAmount.Amount);
        WrappedMoney allowableCostUsed;

        if (isSmallCash && ElectTaxDeferral)
        {
            // Small cash deferral (TCGA 1992 s122)
            // The spinoff allocation portion of cost that relates to cash
            decimal spinoffPercentage = 1m - ParentRetainedPercentage;
            WrappedMoney spinoffAllocationCost = parentCost * spinoffPercentage;
            
            // Defer tax by treating cash as cost reduction
            // If Cash < SpinoffAllocationCost, we just reduce transfer cost by cash amount. Gain = 0.
            // If Cash > SpinoffAllocationCost, excess becomes taxable gain.
            allowableCostUsed = WrappedMoney.Min(cashAmount, spinoffAllocationCost);
            
            // Reduce the transfer cost by cash amount (this is the deferral)
            _transferCost = spinoffAllocationCost - allowableCostUsed;
            
            WrappedMoney excessGain = cashAmount - allowableCostUsed;
            if (excessGain.Amount > 0)
            {
                string calculationDetail = $"Small cash treatment (deferred) from spinoff of {SpinoffCompanyTicker}: \n" +
                                           $"\tCash Received: {cashAmount}\n" +
                                           $"\tSpinoff Allocation Cost: {spinoffAllocationCost}\n" +
                                           $"\tExcess Gain: {excessGain} (taxable)\n" +
                                           $"\tCost transferred to spinoff: {_transferCost}";
                CreateCashDisposal(excessGain, WrappedMoney.GetBaseCurrencyZero(), calculationDetail, section104);
            }
            // Note: No disposal created if no excess gain (fully deferred)
        }
        else
        {
            // Normal matching (part-disposal rules)
            allowableCostUsed = GetCashInLieuCostUsed(parentCost);
            
            // Reduce the transfer cost by the amount used for cash-in-lieu
            _transferCost -= allowableCostUsed;

            string calculationDetail = $"Cash-in-lieu from spinoff of {SpinoffCompanyTicker}: \n" +
                                       $"\tCash Received: {cashAmount}\n" +
                                       $"\tProportioned Allowable Cost: {allowableCostUsed}";

            CreateCashDisposal(cashAmount, allowableCostUsed, calculationDetail, section104);
        }
        
        return allowableCostUsed;
    }

    /// <summary>
    /// Determines if cash-in-lieu qualifies as "small" under TCGA s122.
    /// Small cash is defined as £3000 or 5% of total market value of the shareholding before distribution.
    /// </summary>
    private bool IsSmallCash(decimal cashAmountInBaseCurrency)
    {
        const decimal smallCashThreshold = 3000m;

        if (cashAmountInBaseCurrency <= smallCashThreshold)
            return true;

        // Check 5% rule using parent shareholding value before distribution
        // This is Parent Market Value + Spinoff Market Value (the total value before spinoff)
        if (ParentMarketValue?.BaseCurrencyAmount.Amount > 0 && SpinoffMarketValue?.BaseCurrencyAmount.Amount > 0)
        {
            decimal parentShareholdingValueBeforeDistribution = 
                ParentMarketValue.BaseCurrencyAmount.Amount + SpinoffMarketValue.BaseCurrencyAmount.Amount;
            decimal fivePercentThreshold = parentShareholdingValueBeforeDistribution * 0.05m;
            return cashAmountInBaseCurrency <= fivePercentThreshold;
        }
        return false;
    }

    private void CreateCashDisposal(WrappedMoney proceeds, WrappedMoney allowableCost, string additionalInfo, UkSection104 section104)
    {
        // Look up residency status from section104, default to Resident if not available
        ResidencyStatus residencyStatus = section104.ResidencyStatusRecord?.GetResidencyStatus(DateOnly.FromDateTime(Date))
            ?? ResidencyStatus.Resident;

        CashDisposal = new CorporateActionTaxCalculation(this, proceeds, allowableCost, residencyStatus, additionalInfo);
    }

    private string BuildParentExplanation(UkSection104 section104, decimal quantity, WrappedMoney originalCost, WrappedMoney retainedCost)
    {
        var explanation = new System.Text.StringBuilder();
        explanation.AppendLine($"Spinoff of {SpinoffCompanyTicker} on {Date:d}");
        explanation.AppendLine($"Parent market value: {ParentMarketValue.PrintToTextFile()}");
        explanation.AppendLine($"Spinoff market value: {SpinoffMarketValue.PrintToTextFile()}");
        explanation.AppendLine($"Parent retained percentage: {ParentRetainedPercentage:P2}");
        explanation.AppendLine($"Original cost: {originalCost}, Retained cost: {retainedCost}");
        explanation.AppendLine($"Cost transferred to spinoff: {_transferCost}");
        
        if (CashInLieu != null)
        {
            explanation.AppendLine($"Cash-in-lieu received: {CashInLieu.BaseCurrencyAmount}");
        }

        return explanation.ToString().TrimEnd();
    }

    private void ProcessSpinoffCompany(UkSection104 section104)
    {
        if (_transferQuantity == 0)
        {
            // Nothing to transfer
            return;
        }

        string explanation = $"Spinoff from {AssetName} on {Date:d}: {_transferQuantity} shares received with cost basis of {_transferCost}";

        // AddAssets sets the explanation on the history entry it creates
        section104.AddAssets(Date, _transferQuantity, _transferCost, null, explanation);
    }

    public override string GetDuplicateSignature()
    {
        string cashInfo = CashInLieu != null
            ? $"|{CashInLieu.Amount.Amount}|{CashInLieu.Amount.Currency}"
            : "|NOCASH";
        return $"SPINOFF|{base.GetDuplicateSignature()}|{SpinoffCompanyTicker}|{SpinoffSharesPerParentShare}{cashInfo}";
    }
}
