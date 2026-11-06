using InvestmentTaxCalculator.ViewModel;

using Microsoft.AspNetCore.Components;

using Shouldly;

using System.Reflection;

namespace UnitTest.Test.ViewModel;

public class WorkflowStepsTests
{
    // Pages reached from the sidebar outside the numbered workflow.
    private static readonly string[] PagesOutsideWorkflow = ["", "AcknowledgementPage", "HowItWorks"];

    private static IEnumerable<string> RoutedPages() =>
        typeof(WorkflowSteps).Assembly.GetTypes()
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>())
            .Select(route => route.Template.Trim('/'));

    [Fact]
    public void EveryRoutedPage_AppearsInWorkflowExactlyOnce()
    {
        var linkedPages = WorkflowSteps.All.SelectMany(step => step.Links).Select(link => link.PageAddress).ToList();

        foreach (string page in RoutedPages().Except(PagesOutsideWorkflow, StringComparer.OrdinalIgnoreCase))
        {
            linkedPages.Count(linked => string.Equals(linked, page, StringComparison.OrdinalIgnoreCase))
                .ShouldBe(1, $"Page '{page}' should appear exactly once in the workflow navigation");
        }
    }

    [Fact]
    public void EveryWorkflowLink_PointsToARoutedPage()
    {
        var routes = RoutedPages().ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var link in WorkflowSteps.All.SelectMany(step => step.Links))
        {
            routes.ShouldContain(link.PageAddress, $"Workflow link '{link.Text}' should point to an existing page");
        }
    }

    [Fact]
    public void Steps_AreNumberedInOrder()
    {
        WorkflowSteps.All.Select(step => step.Number).ShouldBe(Enumerable.Range(1, WorkflowSteps.All.Count));
    }

    [Fact]
    public void Calculate_ComesBeforeAdjust_BecauseAdjustPagesNeedSection104Pools()
    {
        var calculate = WorkflowSteps.FindStep(WorkflowSteps.CalculatePage).ShouldNotBeNull();
        var adjust = WorkflowSteps.FindStep("AddExcessReportableIncome").ShouldNotBeNull();

        WorkflowSteps.FindStep(WorkflowSteps.ImportPage).ShouldNotBeNull().Number.ShouldBe(1);
        calculate.Number.ShouldBeLessThan(adjust.Number);
        adjust.IsOptional.ShouldBeTrue();
    }

    [Theory]
    [InlineData("CgtYearlyTaxSummaryPage", 4)]
    [InlineData("cgtyearlytaxsummarypage", 4)]
    [InlineData("/CalculatePage", 2)]
    [InlineData("MainCalculatorPage?x=1", 1)]
    [InlineData("corporate-actions#history", 3)]
    public void FindStep_MatchesRouteIgnoringCaseQueryAndFragment(string path, int expectedStep)
    {
        WorkflowSteps.FindStep(path).ShouldNotBeNull().Number.ShouldBe(expectedStep);
    }

    [Theory]
    [InlineData("")]
    [InlineData("AcknowledgementPage")]
    [InlineData("HowItWorks")]
    [InlineData("SomeUnknownPage")]
    public void FindStep_ReturnsNullOutsideWorkflow(string path)
    {
        WorkflowSteps.FindStep(path).ShouldBeNull();
    }

    [Fact]
    public void NextStep_StartsAtImportAndEndsAfterReport()
    {
        WorkflowSteps.NextStep(null).ShouldBe(WorkflowSteps.All[0]);
        WorkflowSteps.NextStep(WorkflowSteps.All[^1]).ShouldBeNull();
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 4)]
    [InlineData(4, 5)]
    public void NextStep_ReturnsFollowingStep(int index, int nextIndex)
    {
        WorkflowSteps.NextStep(WorkflowSteps.All[index]).ShouldBe(WorkflowSteps.All[nextIndex]);
    }
}
