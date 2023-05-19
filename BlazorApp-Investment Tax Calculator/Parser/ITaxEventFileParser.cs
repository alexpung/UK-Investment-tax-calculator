using Model;

namespace Parser;

public interface ITaxEventFileParser
{
    bool CheckFileValidity(string content);
    TaxEventLists ParseFile(string content);
}