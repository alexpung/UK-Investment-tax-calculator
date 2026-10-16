namespace InvestmentTaxCalculator.Model;

public class OptionCalculationSetting
{
    /// <summary>
    /// A written option is taxed on the full premium in the tax year it is written. If it is closed, assigned or cash settled in a later tax year,
    /// by default the closing cost is not deducted and is listed as a tax refund for the tax year the option was written instead.
    /// When true, the closing cost is deducted directly from the gain of the tax year the option was written and no tax refund is listed.
    /// Written options without a closing trade are still taxed on the full premium.
    /// </summary>
    public bool DeductClosingCostInWrittenTaxYear { get; set; } = false;
}
