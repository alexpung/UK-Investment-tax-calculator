using CapitalGainCalculator.Model.UkTaxModel;
using System.Collections.Generic;

namespace CapitalGainCalculator.Model;
public class UkCalculationResult : CalculationResult
{
    public Dictionary<string, UkSection104> Setion104Pools { get; set; } = new();
}
