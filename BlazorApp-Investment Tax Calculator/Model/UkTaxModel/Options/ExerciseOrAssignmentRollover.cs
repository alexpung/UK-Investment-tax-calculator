using InvestmentTaxCalculator.Model.Interfaces;

namespace InvestmentTaxCalculator.Model.UkTaxModel.Options;

public record ExerciseOrAssignmentRollover(WrappedMoney ProceedsAdjustment, string Comment) : ITradeEvent
{
    public WrappedMoney NetProceedsAdjustment { get; } = ProceedsAdjustment;
    public WrappedMoney AllowableCostAdjustment => WrappedMoney.GetBaseCurrencyZero();
    public string Description => Comment;
}
