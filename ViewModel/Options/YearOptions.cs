using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CapitalGainCalculator.ViewModel.Options;

public partial class YearOptions : ObservableObject
{
    public ObservableCollection<string> Options { get; set; }
    private readonly string _wildOption = "All Years";
    [ObservableProperty]
    private string _selectedYear;

    public YearOptions() : base()
    {
        Options = new ObservableCollection<string> { _wildOption };
        SelectedYear = Options[0];
    }

    public void SetYears(IEnumerable<int> years)
    {
        Options.Clear();
        Options.Add(_wildOption);
        foreach (int year in years) Options.Add(year.ToString());
        SelectedYear = Options[0];
    }

    /// <summary>
    /// Check if a certain year is Selected. Always true if "All Years" selected.
    /// </summary>
    public bool IsSelectedYear(int? year) => SelectedYear switch
    {
        string a when a.Equals(_wildOption) => true,
        null => false,
        _ => int.Parse(SelectedYear) == year,
    };
}
