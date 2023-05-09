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
        return ReadFiles(Directory.GetFiles(folderPath));
    }

    public TaxEventLists ParseFiles(IEnumerable<string> filenames)
    {
        return ReadFiles(filenames);
    }

    private TaxEventLists ReadFiles(IEnumerable<string> filenames)
    {
        TaxEventLists taxEvents = new();
        foreach (string fileName in filenames)
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
