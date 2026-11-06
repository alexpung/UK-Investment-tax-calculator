namespace InvestmentTaxCalculator.ViewModel;

/// <summary>
/// One page of the "How the calculator works" guides.
/// </summary>
/// <param name="Title">Heading of the guide, also used for its card and previous/next links.</param>
/// <param name="Summary">One sentence shown on the guide's card on the index page.</param>
/// <param name="PageAddress">Relative route of the page (no leading slash).</param>
public record HowItWorksGuide(string Title, string Summary, string PageAddress);

/// <summary>
/// The guides explaining how the app applies the UK matching rules, in reading order.
/// The index page lists them as cards and each guide links to the previous and next one.
/// </summary>
public static class HowItWorksGuides
{
    public const string IndexPage = "HowItWorks";

    public static IReadOnlyList<HowItWorksGuide> All { get; } =
    [
        new("Same-day rule",
            "How the app combines a day's buys and sells, whatever time they were made, and matches them first.",
            "HowItWorks/same-day-rule"),
        new("Net proceeds and fees",
            "Why the app uses proceeds after fees, and how fees are split when a trade is matched under several rules.",
            "HowItWorks/net-proceeds"),
        new("30-day (bed and breakfast) rule",
            "How a sale is matched with shares bought back in the following 30 days, and the exceptions.",
            "HowItWorks/30-day-rule"),
        new("Section 104 pool",
            "How the remaining shares are pooled at average cost and how corporate actions change the pool.",
            "HowItWorks/section-104-pool"),
        new("Shares sold before they are bought",
            "How a sale larger than your holding is matched with later purchases.",
            "HowItWorks/short-sales"),
    ];

    public static HowItWorksGuide Find(string pageAddress) =>
        All.FirstOrDefault(guide => string.Equals(guide.PageAddress, pageAddress, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"No guide is registered for '{pageAddress}'.", nameof(pageAddress));

    public static HowItWorksGuide? Previous(HowItWorksGuide guide)
    {
        int index = IndexOf(guide);
        return index > 0 ? All[index - 1] : null;
    }

    public static HowItWorksGuide? Next(HowItWorksGuide guide)
    {
        int index = IndexOf(guide);
        return index < All.Count - 1 ? All[index + 1] : null;
    }

    private static int IndexOf(HowItWorksGuide guide)
    {
        int index = All.ToList().IndexOf(guide);
        return index >= 0 ? index : throw new ArgumentException($"'{guide.PageAddress}' is not a registered guide.", nameof(guide));
    }
}
