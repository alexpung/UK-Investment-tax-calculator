namespace InvestmentTaxCalculator.Components;

public static class InputFormats
{
    /// <summary>
    /// Display format for manually entered money amounts: thousands separators, no forced trailing zeros,
    /// and enough decimal places to avoid hiding precision the user typed (e.g. 1000 -> 1,000; 1234.5 -> 1,234.5).
    /// </summary>
    public const string Money = "#,##0.##########";
}
