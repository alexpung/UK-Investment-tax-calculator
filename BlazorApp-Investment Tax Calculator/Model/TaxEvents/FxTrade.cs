﻿using InvestmentTaxCalculator.Enumerations;

namespace InvestmentTaxCalculator.Model.TaxEvents;

public record FxTrade : Trade
{
    public override AssetCatagoryType AssetType { get; set; } = AssetCatagoryType.FX;
    public override string PrintToTextFile()
    {
        string action = AcquisitionDisposal switch
        {
            TradeType.ACQUISITION => "Acquire",
            TradeType.DISPOSAL => "Dispose",
            _ => throw new NotImplementedException()
        };

        return $"{action} {Quantity:0.##} unit(s) of {AssetName} on {Date:dd-MMM-yyyy HH:mm} for {GrossProceed.PrintToTextFile()}. {nameof(Description)}: {Description}";
    }
}
