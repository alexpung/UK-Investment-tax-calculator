using CsvHelper;
using CsvHelper.Configuration;

using InvestmentTaxCalculator.Model;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace InvestmentTaxCalculator.Parser;

/// <summary>
/// A logical column a CSV parser reads, together with the header names the broker is known to have used for it.
/// Brokers rename headers from time to time (e.g. Trading 212 "Time" -> "Time (UTC)"), so each known name is listed here
/// and files with an unknown header can still be imported by letting the user map the column manually.
/// </summary>
/// <param name="Key">Identifier the parser uses to look up the column. Also shown to the user.</param>
/// <param name="Description">What the column contains, shown to the user when mapping columns.</param>
/// <param name="Required">Whether the parser cannot work without this column.</param>
/// <param name="KnownHeaders">Header names the broker is known to use for this column, matched case-insensitively.</param>
public record CsvColumnDefinition(string Key, string Description, bool Required, IReadOnlyList<string> KnownHeaders);

/// <summary>
/// Maps the logical columns of a parser (<see cref="CsvColumnDefinition.Key"/>) to the actual header names in a CSV file.
/// </summary>
public class CsvColumnMap
{
    private readonly Dictionary<string, string> _keyToHeader;

    public CsvColumnMap() => _keyToHeader = [];

    public CsvColumnMap(IReadOnlyDictionary<string, string> keyToHeader) => _keyToHeader = new(keyToHeader);

    public IReadOnlyDictionary<string, string> Mappings => _keyToHeader;

    /// <summary>The header in the file mapped to the column. Throws if the column is not mapped.</summary>
    public string this[string key] => TryGetHeader(key, out string? header)
        ? header
        : throw new InvalidDataException($"Column '{key}' is needed but is not mapped to any column in the file.");

    public bool TryGetHeader(string key, [NotNullWhen(true)] out string? header) => _keyToHeader.TryGetValue(key, out header);

    /// <summary>Map a column to a header, or remove the mapping when header is null or empty.</summary>
    public void Set(string key, string? header)
    {
        if (string.IsNullOrEmpty(header)) _keyToHeader.Remove(key);
        else _keyToHeader[key] = header;
    }

    public CsvColumnMap Clone() => new(_keyToHeader);
}

/// <summary>
/// A CSV parser whose columns can be mapped by the user when the file headers do not match the known header names.
/// </summary>
public interface IColumnMappableCsvParser : ITaxEventFileParser
{
    /// <summary>Name of the file format shown to the user, e.g. "Trading 212".</summary>
    string FormatName { get; }
    IReadOnlyList<CsvColumnDefinition> Columns { get; }

    /// <summary>
    /// Problems that would stop the file being parsed correctly with the column map, e.g. missing required columns,
    /// or columns only needed for some files such as dividend columns when the file has dividends. Empty if the map can be used.
    /// </summary>
    IReadOnlyList<string> ValidateColumnMap(string data, CsvColumnMap columnMap);

    TaxEventLists ParseFile(string data, CsvColumnMap columnMap);
}

public static class CsvColumnMapper
{
    private static readonly CsvConfiguration _config = new(CultureInfo.InvariantCulture)
    {
        HeaderValidated = null,
        MissingFieldFound = null,
        BadDataFound = null
    };

    public static string[] ReadHeaders(string data)
    {
        using var reader = new StringReader(data);
        using var csv = new CsvReader(reader, _config);
        if (!csv.Read()) return [];
        csv.ReadHeader();
        return csv.HeaderRecord ?? [];
    }

    /// <summary>Read the header and up to <paramref name="maxRows"/> data rows, used to preview values when mapping columns.</summary>
    public static (string[] Headers, List<string[]> Rows) ReadPreview(string data, int maxRows)
    {
        using var reader = new StringReader(data);
        using var csv = new CsvReader(reader, _config);
        if (!csv.Read()) return ([], []);
        csv.ReadHeader();
        List<string[]> rows = [];
        while (rows.Count < maxRows && csv.Read())
        {
            rows.Add(csv.Parser.Record ?? []);
        }
        return (csv.HeaderRecord ?? [], rows);
    }

    /// <summary>
    /// Match each column to a header in the file. A header in <paramref name="preferred"/> takes priority if it exists in the file,
    /// otherwise the first header matching one of the column's known names (case-insensitive, ignoring surrounding spaces) is used.
    /// </summary>
    public static CsvColumnMap AutoMap(IEnumerable<CsvColumnDefinition> columns, IReadOnlyCollection<string> headers, CsvColumnMap? preferred = null)
    {
        CsvColumnMap map = new();
        foreach (CsvColumnDefinition column in columns)
        {
            if (preferred is not null && preferred.TryGetHeader(column.Key, out string? preferredHeader) && headers.Contains(preferredHeader))
            {
                map.Set(column.Key, preferredHeader);
                continue;
            }
            string? match = headers.FirstOrDefault(header => column.KnownHeaders.Any(known => string.Equals(header.Trim(), known, StringComparison.OrdinalIgnoreCase)));
            map.Set(column.Key, match);
        }
        return map;
    }

    public static List<CsvColumnDefinition> GetMissingRequiredColumns(IEnumerable<CsvColumnDefinition> columns, CsvColumnMap map)
        => columns.Where(column => column.Required && !map.TryGetHeader(column.Key, out _)).ToList();

    /// <summary>Whether every required column can be found in the file using the known header names.</summary>
    public static bool CanAutoMap(IReadOnlyList<CsvColumnDefinition> columns, string data)
    {
        try
        {
            return GetMissingRequiredColumns(columns, AutoMap(columns, ReadHeaders(data))).Count == 0;
        }
        catch
        {
            return false;
        }
    }

    public static List<string> GetMissingRequiredColumnMessages(IEnumerable<CsvColumnDefinition> columns, CsvColumnMap map)
        => GetMissingRequiredColumns(columns, map).Select(column => $"Required column '{column.Key}' is not mapped.").ToList();

    public static void ThrowIfInvalid(IColumnMappableCsvParser parser, string data, CsvColumnMap map)
    {
        IReadOnlyList<string> problems = parser.ValidateColumnMap(data, map);
        if (problems.Count > 0)
        {
            throw new InvalidDataException($"Cannot read the file as {parser.FormatName}: {string.Join(" ", problems)}");
        }
    }
}
