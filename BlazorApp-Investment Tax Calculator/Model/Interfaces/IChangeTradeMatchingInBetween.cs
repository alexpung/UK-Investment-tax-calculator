using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

namespace InvestmentTaxCalculator.Model.Interfaces;

public interface IChangeTradeMatchingInBetween
{
    void ChangeTradeMatching(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, TradeMatch tradeMatch);
}