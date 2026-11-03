namespace InvestmentTaxCalculator.ViewModel;

/// <summary>
/// A page reachable from the workflow navigation.
/// </summary>
/// <param name="Text">Link text shown in the sidebar.</param>
/// <param name="PageAddress">Relative route of the page (no leading slash), as used by <c>NavLink</c>.</param>
public record WorkflowLink(string Text, string PageAddress);

/// <summary>
/// One numbered step of the tax workflow, grouping the pages used at that point.
/// </summary>
public record WorkflowStep(int Number, string Title, string Hint, IReadOnlyList<WorkflowLink> Links)
{
    public bool Contains(string relativePath) => Links.Any(link => PathMatches(link.PageAddress, relativePath));

    internal static bool PathMatches(string pageAddress, string relativePath)
    {
        string path = relativePath.Split('?', '#')[0].Trim('/');
        return string.Equals(path, pageAddress, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// The order in which the app is used: import statements, adjust what the import produced,
/// calculate, review the results, check the workings and finally produce the report.
/// The adjust step comes after import because its pages (e.g. bond ETF income selection,
/// ERI and equalisation) choose from tickers that only exist once data has been imported.
/// </summary>
public static class WorkflowSteps
{
    public const string ImportPage = "MainCalculatorPage";
    public const string CalculatePage = "CalculatePage";

    public static IReadOnlyList<WorkflowStep> All { get; } =
    [
        new(1, "Import", "Broker statements or a saved session",
        [
            new("Import data", ImportPage),
        ]),
        new(2, "Adjust", "Optional · uses your imported data",
        [
            new("Income taxable dividend selection", "ConvertPage"),
            new("ERI and Equalisation", "AddExcessReportableIncome"),
            new("Corporate Actions and Events", "corporate-actions"),
            new("Add Manual Entries", "AddTradePage"),
            new("Residency Status", "ResidencyStatusPage"),
        ]),
        new(3, "Calculate", "Run the calculation and export",
        [
            new("Calculate & export", CalculatePage),
        ]),
        new(4, "Review", "Tax summaries",
        [
            new("CGT Yearly Summary", "CgtYearlyTaxSummaryPage"),
            new("Dividend/Income Summary", "DividendYearlyTaxSummaryPage"),
            new("Disposal Summary", "CalculationViewPage"),
        ]),
        new(5, "Check the workings", "Raw data behind the results",
        [
            new("Trade Matches", "TradeMatchPage"),
            new("Imported Trades", "ImportedTradesDataPage"),
            new("Dividend/Income Data", "DividendDataPage"),
            new("Section 104 Data", "Section104DataPage"),
        ]),
        new(6, "Report", "PDF for your records",
        [
            new("PDF Report", "PdfReportPage"),
        ]),
    ];

    /// <summary>
    /// Finds the workflow step containing the page at <paramref name="relativePath"/>, or null for pages
    /// outside the workflow (home, acknowledgements).
    /// </summary>
    public static WorkflowStep? FindStep(string relativePath) => All.FirstOrDefault(step => step.Contains(relativePath));

    /// <summary>
    /// Finds the link for the page at <paramref name="relativePath"/>, or null for pages outside the workflow.
    /// </summary>
    public static WorkflowLink? FindLink(string relativePath) =>
        All.SelectMany(step => step.Links).FirstOrDefault(link => WorkflowStep.PathMatches(link.PageAddress, relativePath));

    /// <summary>
    /// The step after <paramref name="step"/>, or the first step when <paramref name="step"/> is null.
    /// Returns null after the last step.
    /// </summary>
    public static WorkflowStep? NextStep(WorkflowStep? step) =>
        step is null ? All[0] : All.FirstOrDefault(candidate => candidate.Number == step.Number + 1);
}
