using Enumerations;

using Model;
using Model.TaxEvents;

using System.ComponentModel.DataAnnotations;
using System.Globalization;
namespace ViewModel;

public class DividendInputViewModel
{
    [Required]
    public required string AssetName { get; set; }
    [Required]
    public required DateTime Date { get; set; }
    [Required]
    [CustomValidationCompanyLocationString]
    public required string CompanyLocationString { get; set; }
    [Required]
    [CustomValidationCurrencyString]
    public required string CurrencyString { get; set; }
    [Required]
    public decimal GrossAmount { get; set; } = 0;
    [Required]
    public decimal GrossPaymentInLieuAmount { get; set; } = 0;
    [Required]
    public decimal WithHoldingAmount { get; set; } = 0;
    [Required]
    public decimal FxRate { get; set; } = 1;
    public string Description { get; set; } = "";

    public List<Dividend> Convert()
    {
        List<Dividend> result = [];
        if (GrossAmount != 0)
        {
            result.Add(Convert(DividendType.DIVIDEND, GrossAmount));
        }
        if (GrossPaymentInLieuAmount != 0)
        {
            result.Add(Convert(DividendType.DIVIDEND_IN_LIEU, GrossPaymentInLieuAmount));
        }
        if (WithHoldingAmount != 0)
        {
            result.Add(Convert(DividendType.WITHHOLDING, WithHoldingAmount));
        }
        return result;
    }

    private Dividend Convert(DividendType dividendType, decimal amount)
    {
        RegionInfo companyLocation = new(CompanyLocationString);
        WrappedMoney money = new(amount, CurrencyString);
        DescribedMoney describedMoney = new() { Amount = money, Description = Description, FxRate = FxRate };
        return new Dividend()
        {
            AssetName = AssetName,
            Date = Date,
            DividendType = dividendType,
            Proceed = describedMoney,
            CompanyLocation = companyLocation
        };
    }
}

public class CustomValidationCompanyLocationStringAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null) return new ValidationResult("2 letter ISO 3166 code required.");
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

public class CustomValidationCurrencyStringAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null) return new ValidationResult("3 letter ISO 4217 code required.");
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
