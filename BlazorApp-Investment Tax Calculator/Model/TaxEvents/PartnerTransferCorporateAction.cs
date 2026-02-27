using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;

namespace InvestmentTaxCalculator.Model.TaxEvents;

public enum PartnerTransferDirection
{
    GiftToPartner,
    ReceiveFromPartner
}

public record PartnerTransferCorporateAction : CorporateAction, IChangeSection104
{
    public required PartnerTransferDirection Direction { get; init; }
    public required decimal Quantity { get; init; }
    public DescribedMoney? TransferredCost { get; init; }

    public override string Reason => Direction switch
    {
        PartnerTransferDirection.GiftToPartner => $"{AssetName} gift to partner of {Quantity:0.####} shares on {Date:d}",
        PartnerTransferDirection.ReceiveFromPartner => $"{AssetName} received from partner of {Quantity:0.####} shares with transferred cost {TransferredCost?.BaseCurrencyAmount ?? WrappedMoney.GetBaseCurrencyZero()} on {Date:d}",
        _ => throw new NotImplementedException($"Unknown direction {Direction}")
    };

    public override AssetCategoryType AppliesToAssetCategoryType { get; } = AssetCategoryType.STOCK;

    public override MatchAdjustment TradeMatching(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, MatchAdjustment matchAdjustment)
    {
        return matchAdjustment;
    }

    public override void ChangeSection104(UkSection104 section104)
    {
        if (AssetName != section104.AssetName) return;
        if (Quantity <= 0)
        {
            throw new InvalidOperationException($"Partner transfer quantity must be greater than 0 for {AssetName} on {Date:d}.");
        }

        switch (Direction)
        {
            case PartnerTransferDirection.GiftToPartner:
                ApplyGiftToPartner(section104);
                break;
            case PartnerTransferDirection.ReceiveFromPartner:
                ApplyReceiveFromPartner(section104);
                break;
            default:
                throw new NotImplementedException($"Unknown direction {Direction}");
        }
    }

    private void ApplyGiftToPartner(UkSection104 section104)
    {
        if (section104.Quantity <= 0)
        {
            throw new InvalidOperationException($"Cannot gift {AssetName} to partner because Section 104 pool is empty on {Date:d}.");
        }
        if (Quantity > section104.Quantity)
        {
            throw new InvalidOperationException($"Cannot gift {Quantity:0.####} units of {AssetName} because Section 104 pool has only {section104.Quantity:0.####} units on {Date:d}.");
        }

        WrappedMoney removedCost = section104.AcquisitionCostInBaseCurrency * (Quantity / section104.Quantity);
        string explanation = $"Gift to partner: {Quantity:0.####} units removed from Section 104 pool with transferred cost {removedCost}.";
        section104.AddAssets(Date, -Quantity, -removedCost, null, explanation);
    }

    private void ApplyReceiveFromPartner(UkSection104 section104)
    {
        if (TransferredCost == null)
        {
            throw new InvalidOperationException($"Transferred cost must be provided when receiving {AssetName} from partner on {Date:d}.");
        }
        if (TransferredCost.BaseCurrencyAmount.Amount < 0)
        {
            throw new InvalidOperationException($"Transferred cost must not be negative when receiving {AssetName} from partner on {Date:d}.");
        }

        WrappedMoney addedCost = TransferredCost.BaseCurrencyAmount;
        string explanation = $"Received from partner: {Quantity:0.####} units added to Section 104 pool with transferred cost {addedCost}.";
        section104.AddAssets(Date, Quantity, addedCost, null, explanation);
    }

    public override string GetDuplicateSignature()
    {
        decimal transferAmount = TransferredCost?.Amount.Amount ?? 0m;
        string transferCurrency = TransferredCost?.Amount.Currency ?? "NA";
        decimal transferFx = TransferredCost?.FxRate ?? 0m;
        return $"PARTNERTRANSFER|{base.GetDuplicateSignature()}|{Direction}|{Quantity}|{transferAmount}|{transferCurrency}|{transferFx}";
    }
}
