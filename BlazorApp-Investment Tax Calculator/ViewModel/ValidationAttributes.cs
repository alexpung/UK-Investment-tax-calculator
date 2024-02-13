using Model;

using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace ViewModel;

/// <summary>
/// Check if country string is ISO 3166 code. Does not check if entry is blank/null.
/// </summary>
public class CustomValidationCompanyLocationStringAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrEmpty((string)value)) return ValidationResult.Success;
        string inputString = (string)value;
        try
        {
            _ = new RegionInfo(inputString);
            return ValidationResult.Success;
        }
        catch (ArgumentException)
        {
            return new ValidationResult("Incorrect ISO 3166 code. Please enter 2 letter ISO 3166 code e.g. US, GB, JP, HK");
        }
    }
}
/// <summary>
/// Check if the currency string is ISO 4217 code. Does not check if entry is blank/null.
/// </summary>
public class CustomValidationCurrencyStringAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrEmpty((string)value)) return ValidationResult.Success;
        string inputString = (string)value;
        try
        {
            _ = new WrappedMoney(1, inputString);
            return ValidationResult.Success;
        }
        catch (ArgumentException)
        {
            return new ValidationResult("Incorrect ISO 4217 code. Please enter 3 letter ISO 4217 code e.g. USD, JPY, GBP, HKD");
        }
    }
}
