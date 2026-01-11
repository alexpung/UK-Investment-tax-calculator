namespace InvestmentTaxCalculator.Parser;

public static class CsvParserHelper
{
    public static string GetFieldSafe(this CsvHelper.CsvReader csv, string fieldName)
    {
        return csv.GetField(fieldName) ?? throw new InvalidDataException($"Field '{fieldName}' is missing on row {csv.Context.Parser?.Row}."); ;
    }

    public static T GetFieldSafe<T>(this CsvHelper.CsvReader csv, string fieldName)
    {
        return csv.GetField<T>(fieldName) ?? throw new InvalidDataException($"Field '{fieldName}' is missing on row {csv.Context.Parser?.Row}.");
    }
}
