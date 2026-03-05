namespace InvestmentTaxCalculator.Services;

public sealed class ManualEntryTrackerService
{
    private readonly HashSet<int> _manualEntryIds = [];

    public IReadOnlyCollection<int> ManualEntryIds => _manualEntryIds;

    public void Add(int taxEventId)
    {
        _manualEntryIds.Add(taxEventId);
    }

    public void AddRange(IEnumerable<int> taxEventIds)
    {
        foreach (int taxEventId in taxEventIds)
        {
            _manualEntryIds.Add(taxEventId);
        }
    }

    public void Remove(int taxEventId)
    {
        _manualEntryIds.Remove(taxEventId);
    }
}
