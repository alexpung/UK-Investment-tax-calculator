using System.Globalization;

namespace InvestmentTaxCalculator.Parser;

public static class CsvParserHelper
{
    public static string GetFieldSafe(this CsvHelper.CsvReader csv, string fieldName)
    {
        string? fieldValue = csv.GetField(fieldName);
        if (string.IsNullOrEmpty(fieldValue))
        {
            throw new InvalidDataException($"Field '{fieldName}' is missing on row {csv.Context.Parser?.Row}.");
        }
        return fieldValue;
    }

    public static T GetFieldSafe<T>(this CsvHelper.CsvReader csv, string fieldName)
    {
        return csv.GetField<T?>(fieldName) ?? throw new InvalidDataException($"Field '{fieldName}' is missing on row {csv.Context.Parser?.Row}.");
    }

    public static DateTime ParseDateStringToDateTime(this CsvHelper.CsvReader csv, string fieldName)
    {
        bool success = DateOnly.TryParse(csv.GetFieldSafe(fieldName), CultureInfo.InvariantCulture, out DateOnly dateOnly);
        if (!success)
        {
            throw new InvalidDataException($"Invalid date format for field '{fieldName}' on row {csv.Context.Parser?.Row}.");
        }
        return dateOnly.ToDateTime(TimeOnly.MinValue);
    }
}
