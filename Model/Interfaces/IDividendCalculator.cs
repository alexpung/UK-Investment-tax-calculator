using System.Collections.Generic;

namespace CapitalGainCalculator.Model.Interfaces;

public interface IDividendCalculator
{
    public List<DividendSummary> CalculateTax();
}
