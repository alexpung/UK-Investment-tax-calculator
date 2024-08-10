using Enumerations;

namespace Model.TaxEvents;

public record OptionTrade : Trade
{
    public override AssetCatagoryType AssetType { get; set; } = AssetCatagoryType.OPTION;
    public required string Underlying { get; set; }
    public required WrappedMoney StrikePrice { get; set; }
    public required DateTime ExpiryDate { get; set; }
    public required PUTCALL PUTCALL { get; set; }

    public override string PrintToTextFile()
    {
        string baseText = base.PrintToTextFile();
        return $"{baseText}\n\tOption Details: Underlying Asset: {Underlying}, Strike Price: {StrikePrice}, Expiry Date: {ExpiryDate:dd-MMM-yyyy}";
    }
}

public enum PUTCALL
{
    PUT,
    CALL
}
