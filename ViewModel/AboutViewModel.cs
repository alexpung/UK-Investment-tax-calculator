using System.IO;

namespace CapitalGainCalculator.ViewModel;

public class AboutViewModel
{
    public static string GetVersion => "Version: " + System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? string.Empty;

    public static string CopyrightNotice => File.ReadAllText(@".\View\Resources\AuthorNotice.txt");
}
