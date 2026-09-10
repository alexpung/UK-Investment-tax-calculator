using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using System.Runtime.CompilerServices;

namespace InvestmentTaxCalculator.Services;

public enum CalculationTrigger
{
    Manual,
    NavigationRefresh
}

public class TaxCalculationService(
    UkSection104Pools section104Pools,
    IDividendCalculator dividendCalculator,
    DividendCalculationResult dividendCalculationResult,
    TradeCalculationResult tradeCalculationResult,
    IEnumerable<ITradeCalculator> tradeCalculators,
    YearOptions years,
    ITaxYear taxYear,
    ToastService toastService,
    TaxEventLists taxEventLists,
    ShareIdentityRegistry shareIdentityRegistry)
{
    private bool _isCalculating = false;
    public bool IsCalculating => _isCalculating;
    public CalculationTrigger CurrentTrigger { get; private set; } = CalculationTrigger.Manual;
    public event Action? OnStateChanged;

    private (int Count, int IdentityHash) _calculatedEventFingerprint;

    /// <summary>
    /// Whether a calculation has completed at least once. Holdings and Section 104 pools only exist afterwards,
    /// so entry forms show a quantity as pending until this is true.
    /// </summary>
    public bool HasCalculated { get; private set; }

    /// <summary>
    /// Whether tax events have been added, edited or removed since the last completed calculation, so the
    /// quantities on screen no longer reflect the imported data and the user should recalculate.
    /// </summary>
    public bool IsResultStale => HasCalculated && GetEventFingerprint() != _calculatedEventFingerprint;

    /// <summary>
    /// Whether the Section 104 pools reflect the tax events as they stand now. Anything reading a quantity out of
    /// the pools must gate on this rather than on <see cref="HasCalculated"/> alone: once events have changed the
    /// pools still hold the previous result, and an ERI amount computed from it would be frozen into a saved event.
    /// </summary>
    public bool HasCurrentResult => HasCalculated && !IsResultStale;

    /// <summary>
    /// Cheap stand in for "have the tax events changed", computed on demand and allocating nothing.
    /// <para>
    /// The count catches additions and removals; the combined reference identities catch an event being replaced by
    /// a different instance, which is how the UI edits an entry. Reference identity is used rather than the event id
    /// because a record <c>with</c> expression copies the id onto the new instance, so ids alone would miss such an
    /// edit. Mutating a property of an event already in the list is not detected, but the flows that do so also add
    /// an event, which the count catches.
    /// </para>
    /// </summary>
    private (int Count, int IdentityHash) GetEventFingerprint()
    {
        int count = 0;
        int identityHash = 0;
        foreach (TaxEvent taxEvent in taxEventLists.AllEvents)
        {
            count++;
            identityHash ^= RuntimeHelpers.GetHashCode(taxEvent);
        }
        return (count, identityHash);
    }

    public async Task CalculateAsync(CalculationTrigger trigger = CalculationTrigger.Manual)
    {
        if (_isCalculating) return;

        try
        {
            CurrentTrigger = trigger;
            _isCalculating = true;
            // Cleared up front rather than on success: the pools are emptied below, so from here until this run
            // completes there is no result to read. Without this a failed recalculation would leave the previous
            // run's flag set while the pools stand empty, and the quantities would be reported as up to date.
            HasCalculated = false;
            SafeInvokeOnStateChanged();
            section104Pools.Clear();
            tradeCalculationResult.Clear();
            ITradeTaxCalculation.ResetID();
            // Refresh share identities so events added since import (e.g. manual entries and corporate actions
            // entered in the UI) are matched by share identity during the calculation.
            shareIdentityRegistry.RegisterEvents(taxEventLists.AllEvents);

            // Captured before the calculators run, because that is the input this result reflects. An entry the user
            // adds while the calculation is in flight is not in the results, so recording the fingerprint afterwards
            // would report the result as up to date when it is not.
            (int Count, int IdentityHash) inputFingerprint = GetEventFingerprint();

            foreach (ITradeCalculator tradeCalculator in tradeCalculators)
            {
                tradeCalculationResult.SetResult(await Task.Run(tradeCalculator.CalculateTax));
            }

            dividendCalculationResult.SetResult(await Task.Run(dividendCalculator.CalculateTax));
            years.SetYears(GetSelectableYears());

            HasCalculated = true;
            _calculatedEventFingerprint = inputFingerprint;

            toastService.ShowInformation("Calculation completed.");
        }
        catch (Exception ex)
        {
            toastService.ShowException(ex);
        }
        finally
        {
            _isCalculating = false;
            CurrentTrigger = CalculationTrigger.Manual;
            SafeInvokeOnStateChanged();
        }
    }

    private void SafeInvokeOnStateChanged()
    {
        try
        {
            OnStateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TaxCalculationService.OnStateChanged subscriber failed: {ex}");
        }
    }

    private IEnumerable<int> GetSelectableYears()
    {
        IEnumerable<int> taxYearsWithDisposal = tradeCalculationResult.CalculatedTrade
            .Where(trade => trade.AcquisitionDisposal == TradeType.DISPOSAL)
            .Select(trade => taxYear.ToTaxYear(trade.Date))
            .Distinct();

        IEnumerable<int> taxYearsWithDividend = dividendCalculationResult.DividendSummary
            .Select(dividend => dividend.TaxYear)
            .Distinct();

        return taxYearsWithDisposal.Union(taxYearsWithDividend).OrderByDescending(i => i);
    }
}
