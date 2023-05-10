using System.Collections.Generic;

namespace CapitalGainCalculator.Model.Interfaces;

public interface ITradeCalculator
{
    public List<ITradeTaxCalculation> CalculateTax();
}