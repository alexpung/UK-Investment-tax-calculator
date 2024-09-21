using InvestmentTaxCalculator.Enumerations;

using System.Text;

namespace InvestmentTaxCalculator.Model.TaxEvents;

public record OptionTrade : Trade
{
    public override AssetCatagoryType AssetType { get; set; } = AssetCatagoryType.OPTION;
    public required string Underlying { get; set; }
    public required WrappedMoney StrikePrice { get; set; }
    public required DateTime ExpiryDate { get; set; }
    public required PUTCALL PUTCALL { get; set; }
    public required decimal Multiplier { get; set; }
    public Trade? ExeciseOrExecisedTrade { get; set; }

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
        if (ExeciseOrExecisedTrade != null)
        {
            output.AppendLine($"\tAssociated Trade:");
            output.AppendLine($"\t\tExecuted Trade: {ExeciseOrExecisedTrade.PrintToTextFile()}");
        }

        return output.ToString();
    }
}

public enum PUTCALL
{
    PUT,
    CALL
}
