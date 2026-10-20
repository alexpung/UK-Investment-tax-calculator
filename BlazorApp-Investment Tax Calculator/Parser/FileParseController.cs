using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Services;

using Microsoft.AspNetCore.Components.Forms;

namespace InvestmentTaxCalculator.Parser;

/// <summary>Information shown to the user when a CSV file's columns need to be mapped manually.</summary>
/// <param name="Candidates">Each CSV format the file could be, with the columns that could be matched automatically.</param>
/// <param name="Validate">Problems with a choice that would stop the file being imported, empty if it can be imported.</param>
public record CsvColumnMappingRequest(string FileName, string[] Headers, List<string[]> SampleRows, List<CsvColumnMappingChoice> Candidates,
    Func<CsvColumnMappingChoice, IReadOnlyList<string>> Validate);

/// <summary>A CSV format together with how its columns map to the file headers.</summary>
public record CsvColumnMappingChoice(IColumnMappableCsvParser Parser, CsvColumnMap ColumnMap);

public class FileParseController(IEnumerable<ITaxEventFileParser> taxEventFileParsers, ToastService? toastService = null)
{
    public const long MaxFileSize = 1024 * 1024 * 1000; // 1000 MB
    private const int MappingPreviewRowCount = 3;

    // Column mappings confirmed by the user, keyed by format name, so later files with the same layout import without asking again.
    private readonly Dictionary<string, CsvColumnMap> _confirmedColumnMaps = [];

    /// <param name="requestColumnMapping">
    /// Called when no parser recognises a CSV file, so the user can choose its format and map its columns.
    /// Returns null if the user cancels.
    /// </param>
    public async Task<TaxEventLists> ReadFile(IBrowserFile file, Func<CsvColumnMappingRequest, Task<CsvColumnMappingChoice?>>? requestColumnMapping = null)
    {
        TaxEventLists taxEvents = new();
        using (StreamReader sr = new(file.OpenReadStream(MaxFileSize)))
        {
            string data = await sr.ReadToEndAsync();
            string contentType = file.ContentType;
            var parser = taxEventFileParsers.FirstOrDefault(parser => parser.CheckFileValidity(data, contentType));
            if (parser is not null)
            {
                taxEvents.AddData(parser.ParseFile(data));
            }
            else if (!IsCsvFile(file.Name, contentType) || !taxEventFileParsers.OfType<IColumnMappableCsvParser>().Any())
            {
                toastService?.ShowError($"Cannot find suitable parser for {file.Name}");
            }
            else
            {
                await ParseWithColumnMapping(file.Name, data, taxEvents, requestColumnMapping);
            }
        }
        return taxEvents;
    }

    private async Task ParseWithColumnMapping(string fileName, string data, TaxEventLists taxEvents,
        Func<CsvColumnMappingRequest, Task<CsvColumnMappingChoice?>>? requestColumnMapping)
    {
        (string[] headers, List<string[]> sampleRows) = CsvColumnMapper.ReadPreview(data, MappingPreviewRowCount);
        List<IColumnMappableCsvParser> mappableParsers = taxEventFileParsers.OfType<IColumnMappableCsvParser>().ToList();

        CsvColumnMappingChoice? choice = FindConfirmedColumnMap(mappableParsers, headers, data);
        bool isNewMapping = choice is null;
        if (choice is null && requestColumnMapping is not null)
        {
            List<CsvColumnMappingChoice> candidates = mappableParsers
                .Select(parser => new CsvColumnMappingChoice(parser, CsvColumnMapper.AutoMap(parser.Columns, headers, _confirmedColumnMaps.GetValueOrDefault(parser.FormatName))))
                .ToList();
            choice = await requestColumnMapping(new CsvColumnMappingRequest(fileName, headers, sampleRows, candidates,
                candidate => candidate.Parser.ValidateColumnMap(data, candidate.ColumnMap)));
            if (choice is null)
            {
                toastService?.ShowWarning($"Import of {fileName} cancelled as its columns were not mapped.");
                return;
            }
        }
        if (choice is null)
        {
            toastService?.ShowError($"Cannot find suitable parser for {fileName}");
            return;
        }

        taxEvents.AddData(choice.Parser.ParseFile(data, choice.ColumnMap));
        // Only remember a mapping that parsed successfully.
        if (isNewMapping) _confirmedColumnMaps[choice.Parser.FormatName] = choice.ColumnMap.Clone();
    }

    private CsvColumnMappingChoice? FindConfirmedColumnMap(List<IColumnMappableCsvParser> parsers, string[] headers, string data)
    {
        foreach (IColumnMappableCsvParser parser in parsers)
        {
            if (!_confirmedColumnMaps.TryGetValue(parser.FormatName, out CsvColumnMap? confirmed)) continue;
            // Reuse the mapping only if the file has every header the user picked, i.e. it has the same layout.
            if (!confirmed.Mappings.Values.All(headers.Contains)) continue;
            CsvColumnMap map = CsvColumnMapper.AutoMap(parser.Columns, headers, confirmed);
            if (parser.ValidateColumnMap(data, map).Count == 0) return new CsvColumnMappingChoice(parser, map);
        }
        return null;
    }

    private static bool IsCsvFile(string fileName, string contentType)
    {
        if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) return true;
        // application/vnd.ms-excel is also used for binary .xls workbooks, so it is only trusted together with a .csv extension.
        string mimeType = contentType?.Split(';')[0].Trim() ?? string.Empty;
        return !mimeType.Equals("application/vnd.ms-excel", StringComparison.OrdinalIgnoreCase) && CsvParserHelper.IsCsvContentType(contentType);
    }
}
