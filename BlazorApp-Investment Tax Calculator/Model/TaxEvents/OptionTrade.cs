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
    public bool CashSettled { get; set; } = false;

    public override string PrintToTextFile()
    {
        StringBuilder output = new();
        output.AppendLine(base.PrintToTextFile());  // Include base trade details
        output.AppendLine($"\tOption Details:");
        output.AppendLine($"\tUnderlying Asset: {Underlying}");
        output.AppendLine($"\tOption Type: {PUTCALL}");
        output.AppendLine($"\tStrike Price: {StrikePrice}");
        output.AppendLine($"\tExpiry Date: {ExpiryDate:dd-MMM-yyyy}");
        output.AppendLine($"\tTrade Reason: {TradeReason}");

        // Add details about the exercise or assigned trade, if available
        if (ExerciseOrExercisedTrade != null)
        {
            output.AppendLine($"\tAssociated Trade:");
            output.AppendLine($"\t\tExecuted Trade: {ExerciseOrExercisedTrade.PrintToTextFile()}");
        }

        return output.ToString();
    }
}

public enum PUTCALL
{
    PUT,
    CALL
}
