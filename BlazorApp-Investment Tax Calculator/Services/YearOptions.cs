namespace InvestmentTaxCalculator.Services;

public class YearOptions
{
    public List<DropdownYearItems> Options { get; set; } = [];
    private List<int> _selectedOptions = [];
    public List<int> SelectedOptions
    {
        get { return _selectedOptions; }
        set
        {
            if (value is null)
            {
                _selectedOptions = [];
            }
            else
            {
                _selectedOptions = value;
            }
        }
    }

    public void SetYears(IEnumerable<int> years)
    {
        Options = [.. years.Select(year => new DropdownYearItems { Years = year })];
        SelectedOptions = [.. Options.Select(i => i.Years)];
    }
}

public class DropdownYearItems
{
    public int Years { get; set; }
}

