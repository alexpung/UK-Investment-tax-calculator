using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.TaxEvents;

using System.ComponentModel.DataAnnotations;

namespace InvestmentTaxCalculator.ViewModel;

public class TradeInputViewModel
{
    private static int _nextId = 0;
    public int Id { get; init; }
    [Required]
    public required string AssetName { get; set; }
    [Required]
    public required DateTime Date { get; set; }
    [Required(ErrorMessage = "Please enter number of unit that are bought or sold. If you are shorting or selling, enter the absolute number here and set Acquisition/Disposal to disposal")]
    [Range(0, int.MaxValue, MinimumIsExclusive = true,
        ErrorMessage = "Quantity must be greater than 0. If you are shorting or selling, enter the absolute number here and set Acquisition/Disposal to disposal")]
    public required decimal Quantity { get; set; }
    [Required(ErrorMessage = "Please select if you are buying (acquiring) or selling/shorting (disposing) the asset.")]
    public required TradeType AcquisitionDisposal { get; set; }
    [Required(ErrorMessage = "Please select the asset type.")]
    public required AssetCategoryType AssetType { get; set; }
    [Required(ErrorMessage = "Please enter the currency used for the trade")]
    [CustomValidationCurrencyString]
    public required string GrossProceedCurrency { get; set; } = "gbp";
    [Required(ErrorMessage = "Please enter the amount of local currency you get for selling or you paid for buying. Negative value is allowed and indicate negative price.")]
    public required decimal GrossProceed { get; set; }
    [Required(ErrorMessage = "Please enter the exchange rate used to convert to Sterling")]
    [Range(0, int.MaxValue, MinimumIsExclusive = true, ErrorMessage = "Exchange rate must be greater than 0.")]
    public required decimal GrossProceedExchangeRate { get; set; } = 1;
    [CustomValidationCurrencyString]
    public string CommissionCurrency { get; set; } = "gbp";
    [Range(0, int.MaxValue, MinimumIsExclusive = true, ErrorMessage = "Exchange rate must be greater than 0.")]
    public decimal CommissionExchangeRate { get; set; } = 1;
    public decimal CommissionAmount { get; set; } = 0;
    [CustomValidationCurrencyString]
    public string TaxCurrency { get; set; } = "gbp";
    [Range(0, int.MaxValue, MinimumIsExclusive = true, ErrorMessage = "Exchange rate must be greater than 0.")]
    public decimal TaxExchangeRate { get; set; } = 1;
    public decimal TaxAmount { get; set; } = 0;
    [CustomValidationCurrencyString]
    public string ContractValueCurrency { get; set; } = "gbp";
    [Range(0, int.MaxValue, ErrorMessage = "Exchange rate must be greater than 0.")]
    public decimal ContractValueExchangeRate { get; set; } = 1;
    public decimal ContractValueAmount { get; set; } = 0;
    public string Description { get; set; } = "";

    public TradeInputViewModel()
    {
        Id = Interlocked.Increment(ref _nextId);
    }

    public Trade Convert()
    {
        DescribedMoney grossProceedDescribed = new(GrossProceed, GrossProceedCurrency, GrossProceedExchangeRate);
        DescribedMoney contractValueDescribed = new(ContractValueAmount, ContractValueCurrency, ContractValueExchangeRate);
        List<DescribedMoney> expenses = [];
        if (TaxAmount != 0) expenses.Add(new(TaxAmount, TaxCurrency, TaxExchangeRate));
        if (CommissionAmount != 0) expenses.Add(new(CommissionAmount, CommissionCurrency, CommissionExchangeRate));
        return AssetType switch
        {
            AssetCategoryType.STOCK => new Trade()
            {
                AssetName = AssetName,
                Date = Date,
                Quantity = Quantity,
                AcquisitionDisposal = AcquisitionDisposal,
                AssetType = AssetType,
                GrossProceed = grossProceedDescribed,
                Expenses = [.. expenses],
                Description = Description
            },
            AssetCategoryType.FX => new FxTrade()
            {
                AssetName = AssetName,
                Date = Date,
                Quantity = Quantity,
                AcquisitionDisposal = AcquisitionDisposal,
                AssetType = AssetType,
                GrossProceed = grossProceedDescribed,
                Expenses = [.. expenses],
                Description = Description
            },
            AssetCategoryType.FUTURE => new FutureContractTrade()
            {
                AssetName = AssetName,
                Date = Date,
                Quantity = Quantity,
                AcquisitionDisposal = AcquisitionDisposal,
                AssetType = AssetType,
                GrossProceed = new DescribedMoney() { Amount = WrappedMoney.GetBaseCurrencyZero() },
                ContractValue = contractValueDescribed,
                Expenses = [.. expenses],
                Description = Description
            },
            _ => throw new NotImplementedException(),
        };
    }

    public List<string> ValidateError()
    {
        List<string> errorList = [];
        if (AssetType == AssetCategoryType.FUTURE && string.IsNullOrEmpty(ContractValueCurrency))
        {
            errorList.Add("Contract Value Currency is required for a future contract");
        }
        if (AssetType == AssetCategoryType.FUTURE && GrossProceed != 0)
        {
            errorList.Add("For a future contract gross proceeds should be set to 0. Profit and loss is calucated from contract values, taxes and commission");
        }
        return errorList;
    }

    public List<string> ValidateWarning()
    {
        List<string> errorList = [];
        if (CommissionAmount < 0) errorList.Add("Commission is negative and means a rebate, please check if this is correct.");
        if (TaxAmount < 0) errorList.Add("Tax is negative and means a refund, please check if this is correct.");
        if (ContractValueAmount <= 0 && AssetType == AssetCategoryType.FUTURE) errorList.Add("Negative or zero price in future contract, please check if this is correct.");
        return errorList;
    }
}
