using CapitalGainCalculator.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CapitalGainCalculator.Parser;

public class FileParseController
{
    private readonly IEnumerable<ITaxEventFileParser> _taxEventFileParsers;
    public FileParseController(IEnumerable<ITaxEventFileParser> taxEventFileParsers)
    {
        _taxEventFileParsers = taxEventFileParsers;
    }
    public TaxEventLists ParseFolder(string folderPath)
    {
        TaxEventLists taxEvents = new();
        string[] fileEntries = Directory.GetFiles(folderPath);
        foreach (string fileName in fileEntries)
        {
            var parser = _taxEventFileParsers.FirstOrDefault(parser => parser.CheckFileValidity(fileName));
            if (parser != null)
            {
                taxEvents.AddData(parser.ParseFile(fileName));
            }
        }
        return taxEvents;
    }
}
