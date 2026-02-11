using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using System.Text;

namespace InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

/// <summary>
/// Represents a tax calculation for a corporate action that results in a capital gain or loss.
/// This implementation handles scenarios where a corporate action triggers a disposal without a standard trade.
/// </summary>
public class CorporateActionTaxCalculation : ITradeTaxCalculation
{
    public int Id { get; init; }
    public AssetCategoryType AssetCategoryType { get; init; }
    public string AssetName { get; init; }
    public DateTime Date { get; init; }
    public TradeType AcquisitionDisposal { get; init; } = TradeType.DISPOSAL;
    public bool CalculationCompleted { get; private set; } = false;
    public List<TradeMatch> MatchHistory { get; init; } = [];
    public WrappedMoney TotalCostOrProceed { get; }
    public decimal TotalQty { get; }
    public List<Trade> TradeList { get; init; } = [];
    public WrappedMoney UnmatchedCostOrProceed { get; private set; }
    public decimal UnmatchedQty { get; private set; }
    public WrappedMoney TotalProceeds => MatchHistory.Sum(m => m.BaseCurrencyMatchDisposalProceed);
    public WrappedMoney TotalAllowableCost => MatchHistory.Sum(m => m.BaseCurrencyMatchAllowableCost);
    public WrappedMoney Gain => TotalProceeds - TotalAllowableCost;
    public ResidencyStatus ResidencyStatusAtTrade { get; set; } = ResidencyStatus.Resident;
    public DateTime TaxableDate { get; set; }
    public CorporateAction RelatedCorporateAction { get; init; }

    public CorporateActionTaxCalculation(CorporateAction corporateAction, WrappedMoney proceeds, WrappedMoney allowableCost, ResidencyStatus residencyStatus, decimal quantity, string additionalInfo = "")
    {
        Id = ITradeTaxCalculation.GetNextId();
        RelatedCorporateAction = corporateAction;
        AssetName = corporateAction.AssetName;
        Date = corporateAction.Date;
        TaxableDate = corporateAction.Date;
        AssetCategoryType = corporateAction.AppliesToAssetCategoryType;
        TotalCostOrProceed = proceeds;
        UnmatchedCostOrProceed = proceeds;
        TotalQty = quantity;
        UnmatchedQty = quantity;
        ResidencyStatusAtTrade = residencyStatus;

        // Determine taxable status based on residency
        TaxableStatus taxableStatus = residencyStatus == ResidencyStatus.NonResident 
            ? TaxableStatus.NON_TAXABLE 
            : TaxableStatus.TAXABLE;

        // Build a synthetic match for the corporate action
        var match = new TradeMatch
        {
            Date = DateOnly.FromDateTime(corporateAction.Date),
            AssetName = corporateAction.AssetName,
            TradeMatchType = TaxMatchType.CORPORATE_ACTION,
            MatchedSellTrade = this,
            MatchDisposalQty = quantity,
            MatchAcquisitionQty = 0.0m,
            BaseCurrencyMatchDisposalProceed = proceeds,
            BaseCurrencyMatchAllowableCost = allowableCost,
            IsTaxable = taxableStatus,
            AdditionalInformation = additionalInfo
        };

        MatchHistory.Add(match);
        MatchQty(quantity);
    }

    public void MatchQty(decimal demandedQty)
    {
        if (demandedQty > UnmatchedQty + 0.000000000001m)
        {
            throw new ArgumentException($"Demanded quantity {demandedQty} is greater than unmatched quantity {UnmatchedQty}");
        }

        UnmatchedQty -= demandedQty;
        if (UnmatchedQty < 0.000000000001m)
        {
            UnmatchedQty = 0;
            CalculationCompleted = true;
        }

        UnmatchedCostOrProceed = TotalQty > 0 ? TotalCostOrProceed * (UnmatchedQty / TotalQty) : TotalCostOrProceed;
    }

    public void MatchWithSection104(UkSection104 ukSection104)
    {
        // Corporate actions that use this class usually have their matching pre-determined
        // or don't follow standard S104 matching rules in the same way.
        // For now, we don't implement this as it's handled in the constructor for specific cases.
    }

    public string PrintToTextFile()
    {
        StringBuilder output = new();
        output.AppendLine($"Corporate Action: {RelatedCorporateAction.Reason}");
        output.AppendLine($"Proceeds: {TotalCostOrProceed}");
        output.AppendLine($"Allowable Cost: {TotalAllowableCost}");
        output.AppendLine($"Total gain (loss): {Gain}");
        
        output.AppendLine("Matching details:");
        foreach (var match in MatchHistory)
        {
            output.AppendLine($"\t{match.PrintToTextFile().Replace("\n", "\n\t").TrimEnd()}");
        }
        
        return output.ToString();
    }
}
