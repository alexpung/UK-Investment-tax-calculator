using System.Collections.Generic;

namespace CapitalGainCalculator.Model
{
    public interface ITaxAnalyser
    {
        string AnalyseTaxEventsData(IEnumerable<TaxEvent> events);
    }
}