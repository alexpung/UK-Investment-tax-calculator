using InvestmentTaxCalculator.Enumerations;

using System.Text;

namespace InvestmentTaxCalculator.Model.TaxEvents;

public record OptionTrade : Trade
{
    public override AssetCategoryType AssetType { get; set; } = AssetCategoryType.OPTION;
    public required string Underlying { get; set; }
    public required WrappedMoney StrikePrice { get; set; }
    public required DateTime ExpiryDate { get; set; }
    public required PUTCALL PUTCALL { get; set; }
    public required decimal Multiplier { get; set; }
    public Trade? ExerciseOrExercisedTrade { get; set; }
    public SettlementMethods SettlementMethod { get; set; } = SettlementMethods.UNKNOWN;

    public override string PrintToTextFile()
    {
        StringBuilder output = new();
        output.AppendLine(base.PrintToTextFile());  // Include base trade details
        output.AppendLine($"\tOption Details:");
        output.AppendLine($"\tUnderlying Asset: {Underlying}");
        output.AppendLine($"\tOption Type: {PUTCALL}");
        output.AppendLine($"\tStrike Price: {StrikePrice}");
        output.AppendLine($"\tExpiry Date: {ExpiryDate:dd-MMM-yyyy}");
        output.AppendLine($"\tTrade Reason: {TradeReason.GetDescription()}");

        // Add details about the exercise or assigned trade, if available
        if (ExerciseOrExercisedTrade != null)
        {
            output.AppendLine($"\tAssociated Trade:");
            output.AppendLine($"\t\tExecuted Trade: {ExerciseOrExercisedTrade.PrintToTextFile()}");
        }

        return output.ToString();
    }
    public override string GetDuplicateSignature()
    {
        // We skip base.GetDuplicateSignature() because that is Trade's signature which includes GrossProceed.
        // GrossProceed for options can be modified by OptionHelper (e.g. from 0 to a cash settlement amount),
        // which would cause duplicate detection to fail on subsequent imports.
        return $"OPTION|{AssetName}|{Date.Ticks}|{Isin}|{AcquisitionDisposal}|{Quantity}|{Underlying}|{StrikePrice.Amount}|{StrikePrice.Currency}|{ExpiryDate.Ticks}|{PUTCALL}|{Multiplier}";
    }

    public override string ToSummaryString() => $"Option: {AssetName} ({Date.ToShortDateString()}) - {Quantity} {AcquisitionDisposal}";
}

public enum PUTCALL
{
    PUT,
    CALL
}

public enum SettlementMethods
{
    UNKNOWN,
    CASH,
    // Settled by delivery of the underlying asset
    DELIVERY
}
