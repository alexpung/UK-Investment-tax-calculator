using Enumerations;

using Model;
using Model.TaxEvents;

using System.ComponentModel.DataAnnotations;

using TaxEvents;

namespace ViewModel;

public class TradeInputViewModel
{
    private static int _nextId = 0;
    public int Id { get; init; }
    [Required]
    public required string AssetName { get; set; }
    [Required]
    public required DateTime Date { get; set; }
    [Required(ErrorMessage = "Please enter number of unit that are bought or sold. If you are shorting or selling, enter the absolute number here and set Acquisition/Disposal to disposal")]
    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be greater than 0. If you are shorting or selling, enter the absolute number here and set Acquisition/Disposal to disposal")]
    public required decimal Quantity { get; set; }
    [Required(ErrorMessage = "Please select if you are buying (acquiring) or selling/shorting (disposing) the asset.")]
    public required TradeType AcquisitionDisposal { get; set; }
    [Required(ErrorMessage = "Please select the asset type.")]
    public required AssetCatagoryType AssetType { get; set; }
    [Required(ErrorMessage = "Please enter the currency used for the trade")]
    [CustomValidationCurrencyString]
    public required string GrossProceedCurrency { get; set; } = "gbp";
    [Required(ErrorMessage = "Please enter the amount of local currency you get for selling or you paid for buying. Negative value is allowed and indicate negative price.")]
    public required decimal GrossProceed { get; set; }
    [Required(ErrorMessage = "Please enter the exchange rate used to convert to Sterling")]
    [Range(0, int.MaxValue, ErrorMessage = "Exchange rate must be greater than 0.")]
    public required decimal GrossProceedExchangeRate { get; set; }
    [CustomValidationCurrencyString]
    public string CommissionCurrency { get; set; } = "gbp";
    [Range(0, int.MaxValue, ErrorMessage = "Exchange rate must be greater than 0.")]
    public decimal CommissionExchangeRate { get; set; } = 1;
    public decimal CommissionAmount { get; set; } = 0;
    [CustomValidationCurrencyString]
    public string TaxCurrency { get; set; } = "gbp";
    [Range(0, int.MaxValue, ErrorMessage = "Exchange rate must be greater than 0.")]
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
        WrappedMoney grossProceed = new(GrossProceed, GrossProceedCurrency);
        DescribedMoney grossProceedDescribed = new() { Amount = grossProceed, FxRate = GrossProceedExchangeRate };
        WrappedMoney contractValue = new(ContractValueAmount, ContractValueCurrency);
        DescribedMoney contractValueDescribed = new() { Amount = contractValue, FxRate = ContractValueExchangeRate };
        switch (AssetType)
        {
            case AssetCatagoryType.STOCK:
                return new Trade()
                {
                    AssetName = AssetName,
                    Date = Date,
                    Quantity = Quantity,
                    AcquisitionDisposal = AcquisitionDisposal,
                    AssetType = AssetType,
                    GrossProceed = grossProceedDescribed
                };
            case AssetCatagoryType.FX:
                return new FxTrade()
                {
                    AssetName = AssetName,
                    Date = Date,
                    Quantity = Quantity,
                    AcquisitionDisposal = AcquisitionDisposal,
                    AssetType = AssetType,
                    GrossProceed = grossProceedDescribed
                };
            case AssetCatagoryType.FUTURE:
                return new FutureContractTrade()
                {
                    AssetName = AssetName,
                    Date = Date,
                    Quantity = Quantity,
                    AcquisitionDisposal = AcquisitionDisposal,
                    AssetType = AssetType,
                    GrossProceed = grossProceedDescribed,
                    ContractValue = contractValueDescribed
                };
            default: throw new NotImplementedException();
        }
    }
}
