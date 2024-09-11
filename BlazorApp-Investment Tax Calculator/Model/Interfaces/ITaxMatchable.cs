using InvestmentTaxCalculator.Enumerations;

namespace InvestmentTaxCalculator.Model.Interfaces;

public interface ITaxMatchable : IAssetDatedEvent
{
    public TradeType AcquisitionDisposal { get; init; }
}
