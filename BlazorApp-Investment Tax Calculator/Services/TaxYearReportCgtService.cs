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
        WrappedMoney capitalLossRunningCount = WrappedMoney.GetBaseCurrencyZero();
        foreach (int taxYear in taxYears)
        {
            WrappedMoney totalGainInYear = tradeCalculationResult.GetTotalGain([taxYear]);
            WrappedMoney totalLossInYear = tradeCalculationResult.GetTotalLoss([taxYear]); // Is a negative value
            WrappedMoney netGainInYear = totalGainInYear + totalLossInYear;
            WrappedMoney capitalGainAllowance = _ukCapitalGainAllowance.GetTaxAllowance(taxYear);
            WrappedMoney lossBroughtForward = WrappedMoney.GetBaseCurrencyZero();
            WrappedMoney lossesAvailableToBroughtForward = capitalLossRunningCount;
            WrappedMoney capitalLossInPreviousYears = capitalLossRunningCount;

            if (netGainInYear.Amount < 0)
            {
                lossesAvailableToBroughtForward += netGainInYear * -1;
            }
            else if (netGainInYear >= capitalGainAllowance)
            {
                lossBroughtForward = WrappedMoney.Min(capitalLossInPreviousYears, netGainInYear - capitalGainAllowance);
                lossesAvailableToBroughtForward -= lossBroughtForward;
            }
            capitalLossRunningCount = lossesAvailableToBroughtForward;
            yield return new TaxYearCgtReport()
            {
                TaxYear = taxYear,
                CapitalGainAllowance = capitalGainAllowance,
                TotalGainInYear = totalGainInYear,
                TotalLossInYear = totalLossInYear,
                NetCapitalGain = totalGainInYear + totalLossInYear,
                CgtAllowanceBroughtForwardAndUsed = lossBroughtForward,
                TaxableGainAfterAllowanceAndLossOffset = WrappedMoney.Max(netGainInYear - capitalGainAllowance - lossBroughtForward, WrappedMoney.GetBaseCurrencyZero()),
                LossesAvailableToBroughtForward = lossesAvailableToBroughtForward,
                AvailableCapitalLossesFromPreviousYears = capitalLossInPreviousYears,
            };
        }

    }
}
