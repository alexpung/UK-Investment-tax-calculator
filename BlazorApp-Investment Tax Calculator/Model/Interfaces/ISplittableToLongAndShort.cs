using InvestmentTaxCalculator.Enumerations;

namespace InvestmentTaxCalculator.Model.Interfaces;
public interface ISplittableToLongAndShort<out T>
{
    public PositionType PositionType { get; set; }
    public T SplitTrade(decimal qty);
}