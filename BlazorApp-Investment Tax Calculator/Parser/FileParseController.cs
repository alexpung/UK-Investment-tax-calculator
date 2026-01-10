using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Services;

using Microsoft.AspNetCore.Components.Forms;

namespace InvestmentTaxCalculator.Parser;

public class FileParseController(IEnumerable<ITaxEventFileParser> taxEventFileParsers, ToastService? toastService = null)
{
    private const long _maxFileSize = 1024 * 1024 * 100; // 100 MB

    public async Task<TaxEventLists> ReadFile(IBrowserFile file)
    {
        TaxEventLists taxEvents = new();
        using (StreamReader sr = new(file.OpenReadStream(_maxFileSize)))
        {
            string data = await sr.ReadToEndAsync();
            string contentType = file.ContentType;
            var parser = taxEventFileParsers.FirstOrDefault(parser => parser.CheckFileValidity(data, contentType));
            if (parser is null)
            {
                toastService?.ShowError($"Cannot find suitable parser for {file.Name}");
            }
            else
            {
                taxEvents.AddData(parser.ParseFile(data));
            }
        }
        return taxEvents;
    }
}
