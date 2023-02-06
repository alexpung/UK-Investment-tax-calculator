using CapitalGainCalculator.Model;
using System.Collections.Generic;

namespace CapitalGainCalculator.Parser
{
    public interface ITaxEventFileParser
    {
        bool CheckFileValidity(string fileUri);
        IList<TaxEvent> ParseFile(string fileUri);
    }
}