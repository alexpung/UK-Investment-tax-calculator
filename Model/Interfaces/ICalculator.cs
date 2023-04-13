using System.Collections.Generic;

namespace CapitalGainCalculator.Model.Interfaces;

public interface ICalculator
{
    public void AddTaxEvents(TaxEventLists taxEventLists);
    public List<TradeTaxCalculation> CalculateTax();
}