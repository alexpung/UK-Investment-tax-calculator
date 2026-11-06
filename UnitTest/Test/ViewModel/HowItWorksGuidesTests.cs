using InvestmentTaxCalculator.ViewModel;

using Microsoft.AspNetCore.Components;

using Shouldly;

using System.Reflection;

namespace UnitTest.Test.ViewModel;

public class HowItWorksGuidesTests
{
    private static HashSet<string> RoutedPages() =>
        typeof(HowItWorksGuides).Assembly.GetTypes()
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>())
            .Select(route => route.Template.Trim('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void IndexAndEveryGuide_PointToARoutedPage()
    {
        var routes = RoutedPages();
        routes.ShouldContain(HowItWorksGuides.IndexPage);
        foreach (var guide in HowItWorksGuides.All)
        {
            routes.ShouldContain(guide.PageAddress, $"Guide '{guide.Title}' should point to an existing page");
        }
    }

    [Fact]
    public void EveryRoutedGuidePage_IsRegistered()
    {
        var registered = HowItWorksGuides.All.Select(guide => guide.PageAddress).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (string route in RoutedPages().Where(route => route.StartsWith(HowItWorksGuides.IndexPage + "/", StringComparison.OrdinalIgnoreCase)))
        {
            registered.ShouldContain(route, $"Guide page '{route}' should be listed in HowItWorksGuides so it gets a card and previous/next links");
        }
    }

    [Fact]
    public void PreviousAndNext_WalkTheGuidesInOrder()
    {
        var guides = HowItWorksGuides.All;
        HowItWorksGuides.Previous(guides[0]).ShouldBeNull();
        HowItWorksGuides.Next(guides[^1]).ShouldBeNull();
        for (int i = 1; i < guides.Count; i++)
        {
            HowItWorksGuides.Next(guides[i - 1]).ShouldBe(guides[i]);
            HowItWorksGuides.Previous(guides[i]).ShouldBe(guides[i - 1]);
        }
    }

    [Fact]
    public void Find_IgnoresCaseAndRejectsUnknownPages()
    {
        HowItWorksGuides.Find("howitworks/same-day-rule").ShouldBe(HowItWorksGuides.All[0]);
        Should.Throw<ArgumentException>(() => HowItWorksGuides.Find("HowItWorks/unknown"));
    }
}
