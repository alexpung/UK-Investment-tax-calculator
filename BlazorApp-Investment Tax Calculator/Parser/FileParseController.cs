using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Services;

using Microsoft.AspNetCore.Components.Forms;

namespace InvestmentTaxCalculator.Parser;

public class FileParseController(IEnumerable<ITaxEventFileParser> taxEventFileParsers, ToastService? toastService = null)
{
    public const long MaxFileSize = 1024 * 1024 * 1000; // 1000 MB

    public async Task<TaxEventLists> ReadFile(IBrowserFile file)
    {
        TaxEventLists taxEvents = new();
        using (StreamReader sr = new(file.OpenReadStream(MaxFileSize)))
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
