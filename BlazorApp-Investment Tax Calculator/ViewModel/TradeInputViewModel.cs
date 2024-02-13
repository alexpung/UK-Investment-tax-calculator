using Enumerations;

using System.ComponentModel.DataAnnotations;

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
    public required AssetCatagoryType AssetCatagoryType { get; set; }
    [Required]
    [CustomValidationCurrencyString]
    public required string TransactionCurrency { get; set; }
    [Required]
    public required decimal GrossProceed { get; set; }
    [CustomValidationCurrencyString]
    public string CommissionCurrency { get; set; } = "";
    public decimal CommissionAmount { get; set; } = 0;
    [CustomValidationCurrencyString]
    public string TaxCurrency { get; set; } = "";
    public decimal TaxAmount { get; set; } = 0;
    [CustomValidationCurrencyString]
    public string ContractValueCurrency { get; set; } = "";
    public decimal ContractValueAmount { get; set; } = 0;
    public string Description { get; set; } = "";

    public TradeInputViewModel()
    {
        Id = Interlocked.Increment(ref _nextId);
    }
}
