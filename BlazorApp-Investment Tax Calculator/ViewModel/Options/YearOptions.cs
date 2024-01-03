using CommunityToolkit.Mvvm.Messaging;
using ViewModel.Messages;

namespace ViewModel.Options;

public class YearOptions(IMessenger messenger)
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
            messenger.Send<YearSelectionChangedMessage>();
        }
    }

    public void SetYears(IEnumerable<int> years)
    {
        Options = years.Select(year => new DropdownYearItems { Years = year }).ToList();
        SelectedOptions = Options.Select(i => i.Years).ToList();
    }
}

public class DropdownYearItems
{
    public int Years { get; set; }
}

