using InvestmentTaxCalculator.Enumerations;

namespace InvestmentTaxCalculator.Model.Interfaces;
public interface ISplittableToLongAndShort<out T>
{
    PositionType PositionType { get; set; }
    public T SplitTrade(decimal qty);
}