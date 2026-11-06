namespace InvestmentTaxCalculator.ViewModel;

/// <summary>
/// One page of the "How the calculator works" guides.
/// </summary>
/// <param name="Title">Heading of the guide, also used for its card and previous/next links.</param>
/// <param name="Summary">One sentence shown on the guide's card on the index page.</param>
/// <param name="PageAddress">Relative route of the page (no leading slash).</param>
/// <param name="Section">Heading the guide is listed under on the index page.</param>
public record HowItWorksGuide(string Title, string Summary, string PageAddress, string Section);

/// <summary>
/// The guides explaining how the app applies UK tax rules, in reading order. They replace the project wiki.
/// The index page lists them as cards grouped by <see cref="HowItWorksGuide.Section"/>, and each guide links to the
/// previous and next one.
/// </summary>
public static class HowItWorksGuides
{
    public const string IndexPage = "HowItWorks";

    public const string MatchingRules = "Matching rules";
    public const string TradeTypes = "Trades by asset type";
    public const string Income = "Dividends and income";
    public const string CorporateActions = "Corporate actions and share identity";
    public const string Circumstances = "Residency and limitations";

    /// <summary>Section headings in the order they appear on the index page.</summary>
    public static IReadOnlyList<string> Sections { get; } = [MatchingRules, TradeTypes, Income, CorporateActions, Circumstances];

    public static IReadOnlyList<HowItWorksGuide> All { get; } =
    [
        new("Same-day rule",
            "How the app combines a day's buys and sells, whatever time they were made, and matches them first.",
            "HowItWorks/same-day-rule", MatchingRules),
        new("Net proceeds and fees",
            "Why the app uses proceeds after fees, and how fees are split when a trade is matched under several rules.",
            "HowItWorks/net-proceeds", MatchingRules),
        new("30-day (bed and breakfast) rule",
            "How a sale is matched with shares bought back in the following 30 days, and the exceptions.",
            "HowItWorks/30-day-rule", MatchingRules),
        new("Section 104 pool",
            "How the remaining shares are pooled at average cost and how corporate actions change the pool.",
            "HowItWorks/section-104-pool", MatchingRules),
        new("Shares sold before they are bought",
            "How a sale larger than your holding is matched with later purchases.",
            "HowItWorks/short-sales", MatchingRules),

        new("Shares and funds",
            "Which trades count as shares or funds, where they come from, and how they are reported.",
            "HowItWorks/shares-and-funds", TradeTypes),
        new("Foreign currency",
            "How cash held in other currencies is treated as an asset that is bought and sold.",
            "HowItWorks/foreign-currency", TradeTypes),
        new("Options",
            "Buying, writing, closing, exercise, assignment, expiry and cash settlement of traded options.",
            "HowItWorks/options", TradeTypes),
        new("Futures",
            "How long and short futures positions are matched and how the gain on the contract value is taxed.",
            "HowItWorks/futures", TradeTypes),

        new("Dividends",
            "Which dividends are taxable, how withholding tax and the country are recorded, and bond fund income.",
            "HowItWorks/dividends", Income),
        new("Interest and bond income",
            "Broker interest, bond coupons, accrued income and margin interest.",
            "HowItWorks/interest", Income),
        new("Excess reportable income and equalisation",
            "How reporting fund income is added and how it changes the Section 104 cost.",
            "HowItWorks/reporting-funds", Income),

        new("Stock splits",
            "How splits and reverse splits change the pool, and how cash in lieu of fractions is taxed.",
            "HowItWorks/stock-splits", CorporateActions),
        new("Takeovers and spin-offs",
            "How cost is carried to new shares, split between companies, and what happens to cash received.",
            "HowItWorks/takeovers-and-spin-offs", CorporateActions),
        new("Return of capital",
            "How a return of capital reduces the cost of your holding instead of being taxed as a dividend.",
            "HowItWorks/return-of-capital", CorporateActions),
        new("Transfers with a spouse or civil partner",
            "How shares given to or received from a spouse or civil partner move in and out of the pool.",
            "HowItWorks/partner-transfers", CorporateActions),
        new("Share identities",
            "How the app decides which trades are the same share across tickers, ISINs and renames.",
            "HowItWorks/share-identities", CorporateActions),

        new("Residency status",
            "How resident, non-resident and temporary non-resident periods change what is taxed.",
            "HowItWorks/residency", Circumstances),
        new("What the calculator does not handle",
            "Rules and situations the app does not cover, so you can check them yourself.",
            "HowItWorks/limitations", Circumstances),
    ];

    public static IEnumerable<HowItWorksGuide> InSection(string section) => All.Where(guide => guide.Section == section);

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
