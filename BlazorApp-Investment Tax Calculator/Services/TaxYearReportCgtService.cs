namespace InvestmentTaxCalculator.Services;

using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.UkTaxModel;

public class TaxYearReportService(TradeCalculationResult tradeCalculationResult, ITaxYear taxYearConverter)
{
    private readonly UkCapitalGainAllowance _ukCapitalGainAllowance = new();
    public IEnumerable<TaxYearCgtReport> GetTaxYearReports()
    {
        List<int> taxYears = [.. tradeCalculationResult.CalculatedTrade.Select(trade => taxYearConverter.ToTaxYear(trade.Date))
                                                                                                        .Distinct()
                                                                                                        .Order()];
        decimal capitalLossInPreviousYears = 0m;
        foreach (int taxYear in taxYears)
        {
            decimal totalGainInYear = tradeCalculationResult.GetTotalGain([taxYear]).Amount;
            decimal totalLossInYear = tradeCalculationResult.GetTotalLoss([taxYear]).Amount; // Is a negative value
            decimal netGainInYear = totalGainInYear + totalLossInYear;
            decimal capitalGainAllowance = _ukCapitalGainAllowance.GetTaxAllowance(taxYear);
            decimal lossBroughtForward = 0m;
            decimal lossesAvailableToBroughtForward = capitalLossInPreviousYears;

            if (netGainInYear < 0)
            {
                lossesAvailableToBroughtForward += netGainInYear * -1;
            }
            if (netGainInYear >= capitalGainAllowance)
            {
                lossBroughtForward = Math.Min(capitalLossInPreviousYears, netGainInYear - capitalGainAllowance);
                lossesAvailableToBroughtForward -= lossBroughtForward;
            }

            yield return new TaxYearCgtReport()
            {
                TaxYear = taxYear,
                CapitalGainAllowance = capitalGainAllowance,
                TotalGainInYear = totalGainInYear,
                TotalLossInYear = totalLossInYear,
                NetCapitalGain = totalGainInYear + totalLossInYear,
                CgtAllowanceBroughtForwardAndUsed = lossBroughtForward,
                TaxableGainAfterAllowanceAndLossOffset = Math.Max(netGainInYear - capitalGainAllowance - lossBroughtForward, 0),
                LossesAvailableToBroughtForward = lossesAvailableToBroughtForward
            };
        }

    }
}
