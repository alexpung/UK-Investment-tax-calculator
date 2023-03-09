using System.Collections.Generic;

namespace CapitalGainCalculator.Model.Interfaces;

public interface ITaxAnalyser
{
    string AnalyseTaxEventsData(IEnumerable<TaxEvent> events);
}