using System.Reflection;

namespace InvestmentTaxCalculator.Services;

public static class AppVersionInfo
{
    public static string Current { get; } = GetAppVersion();

    private static string GetAppVersion()
    {
        string? informationalVersion = typeof(AppVersionInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrEmpty(informationalVersion)) return "unknown";
        // strip the source revision suffix (e.g. 1.1.0+abc123) that the SDK may append
        int suffixStart = informationalVersion.IndexOf('+');
        return suffixStart >= 0 ? informationalVersion[..suffixStart] : informationalVersion;
    }
}
