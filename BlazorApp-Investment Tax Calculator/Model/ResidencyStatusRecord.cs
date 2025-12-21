using System;
using System.Collections.Generic;
using System.Linq;
using ResidencyEnum = InvestmentTaxCalculator.Enumerations.ResidencyStatus;

namespace InvestmentTaxCalculator.Model;

public class ResidencyStatusRecord
{
    public record RangeEntry(DateOnly Start, DateOnly End, ResidencyEnum Status);

    // Default all dates to Resident by providing a single full-range entry.
    public List<RangeEntry> Ranges { get; set; } =
    [
        new RangeEntry(DateOnly.MinValue, DateOnly.MaxValue, ResidencyEnum.Resident)
    ];

    public void SetResidencyStatus(DateOnly startDate, DateOnly endDate, ResidencyEnum residencyStatus)
    {
        if (startDate > endDate)
            throw new ArgumentException("Start date must be on or before end date.");

        var intermediateRanges = new List<RangeEntry>();

        // Step 1: Fragment existing ranges
        foreach (var r in Ranges)
        {
            if (r.End < startDate || r.Start > endDate)
            {
                intermediateRanges.Add(r);
            }
            else
            {
                // Left fragment
                if (r.Start < startDate)
                {
                    intermediateRanges.Add(new RangeEntry(r.Start, startDate.AddDays(-1), r.Status));
                }
                // Right fragment
                if (r.End > endDate)
                {
                    intermediateRanges.Add(new RangeEntry(endDate.AddDays(1), r.End, r.Status));
                }
            }
        }

        intermediateRanges.Add(new RangeEntry(startDate, endDate, residencyStatus));

        // Step 2: Merge and Normalize
        Ranges.Clear();
        var sortedRanges = intermediateRanges.OrderBy(r => r.Start);

        foreach (var current in sortedRanges)
        {
            if (Ranges.Count == 0)
            {
                Ranges.Add(current);
                continue;
            }

            var last = Ranges[^1];

            // Check if current can be merged into last
            if (current.Status == last.Status && current.Start <= last.End.AddDays(1))
            {
                // Update the end date of the last range in the list
                Ranges[^1] = new RangeEntry(last.Start, current.End > last.End ? current.End : last.End, last.Status);
            }
            else
            {
                Ranges.Add(current);
            }
        }
    }

    /// <summary>
    /// Return the residency status for the given date. If no range matches, returns <see cref="ResidencyEnum.Resident"/> by default.
    /// </summary>
    public ResidencyEnum GetResidencyStatus(DateOnly date)
    {
        var match = Ranges.FirstOrDefault(r => r.Start <= date && date <= r.End);
        return match?.Status ?? ResidencyEnum.Resident;
    }
}
