using Microsoft.AspNetCore.Components.Forms;
using Model;

namespace Parser;

public class FileParseController
{
    private readonly IEnumerable<ITaxEventFileParser> _taxEventFileParsers;

    public FileParseController(IEnumerable<ITaxEventFileParser> taxEventFileParsers)
    {
        _taxEventFileParsers = taxEventFileParsers;
    }

    public TaxEventLists ReadFile(IBrowserFile file)
    {
        TaxEventLists taxEvents = new();
        string content = new StreamReader(file.OpenReadStream()).ReadToEnd();
        var parser = _taxEventFileParsers.FirstOrDefault(parser => parser.CheckFileValidity(content));
        if (parser != null)
        {
            taxEvents.AddData(parser.ParseFile(content));
        }
        return taxEvents;
    }
}
