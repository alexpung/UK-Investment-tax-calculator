﻿using Model.UkTaxModel.Stocks;

namespace Model.Interfaces;

public interface IChangeTradeMatchingInBetween
{
    void ChangeTradeMatching(ITradeTaxCalculation trade1, ITradeTaxCalculation trade2, TradeMatch tradeMatch);
}