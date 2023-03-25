using CapitalGainCalculator.Model;

namespace CapitalGainCalculator.Parser;

public interface ITaxEventFileParser
{
    bool CheckFileValidity(string fileUri);
    TaxEventLists ParseFile(string fileUri);
}