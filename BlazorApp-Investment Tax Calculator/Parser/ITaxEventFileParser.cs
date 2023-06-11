using Model;

namespace Parser;

public interface ITaxEventFileParser
{
    bool CheckFileValidity(string data, string contentType);
    TaxEventLists ParseFile(string data);
}