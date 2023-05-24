using Microsoft.AspNetCore.Components.Forms;
using Model;

namespace Parser;

public class FileParseController
{
    private const long _maxFileSize = 1024 * 1024 * 100; // 100 MB
    private readonly IEnumerable<ITaxEventFileParser> _taxEventFileParsers;

    public FileParseController(IEnumerable<ITaxEventFileParser> taxEventFileParsers)
    {
        _taxEventFileParsers = taxEventFileParsers;
    }

    public async Task<TaxEventLists> ReadFile(IBrowserFile file)
    {
        TaxEventLists taxEvents = new();
        using (StreamReader sr = new(file.OpenReadStream(_maxFileSize)))
        {
            string data = await sr.ReadToEndAsync();
            string contentType = file.ContentType;
            var parser = _taxEventFileParsers.FirstOrDefault(parser => parser.CheckFileValidity(data, contentType));
            if (parser != null)
            {
                taxEvents.AddData(parser.ParseFile(data));
            }
        }
        return taxEvents;
    }
}
