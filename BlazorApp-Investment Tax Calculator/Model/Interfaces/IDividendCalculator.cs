using System.Collections.Generic;

namespace Model.Interfaces;

public interface IDividendCalculator
{
    public List<DividendSummary> CalculateTax();
}
