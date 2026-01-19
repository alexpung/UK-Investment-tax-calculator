using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;

namespace InvestmentTaxCalculator.Model.TaxEvents;

public record ReturnOfCapitalCorporateAction : CorporateAction, IChangeSection104
{
    // Amount of return of capital (with fx / base currency info)
    public required DescribedMoney Amount { get; init; }

    public override string Reason => $"{AssetName} return of capital of {Amount.BaseCurrencyAmount} on {Date:d}\n";

    // Return of capital increases the acquisition cost (adjust Section 104 pool)
    public override void ChangeSection104(UkSection104 section104)
    {
        if (AssetName != section104.AssetName) return;
        string explanation = $"Return of capital of {Amount.BaseCurrencyAmount} on {Date:d}";
        section104.AdjustAcquisitionCost(Amount.BaseCurrencyAmount * -1, Date, explanation);
    }

    // Return of capital does not affect matching quantities — leave factor unchanged
    // same day: no adjustment
    // bed and breakfast: since disposal comes first then ex dividend date, the acquisition is made after the ex dividend date, so no adjustment
    public override MatchAdjustment TradeMatching(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, MatchAdjustment matchAdjustment)
    {
        return matchAdjustment;
    }

    public override string GetDuplicateSignature()
    {
        return $"ROC|{base.GetDuplicateSignature()}|{Amount.Amount.Amount}|{Amount.Amount.Currency}";
    }
}
