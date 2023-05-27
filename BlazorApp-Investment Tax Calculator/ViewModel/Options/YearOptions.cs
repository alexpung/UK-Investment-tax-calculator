using CommunityToolkit.Mvvm.Messaging;
using ViewModel.Messages;

namespace ViewModel.Options;

public class YearOptions
{
    public List<DropdownYearItems> Options { get; set; } = new();
    private List<int> _selectedOptions = new();
    public List<int> SelectedOptions
    {
        get { return _selectedOptions; }
        set
        {
            if (value is null)
            {
                _selectedOptions = new List<int>();
            }
            else
            {
                _selectedOptions = value;
            }
            _messenger.Send<YearSelectionChangedMessage>();
        }
    }
    private readonly IMessenger _messenger;

    public YearOptions(IMessenger messenger)
    {
        _messenger = messenger;
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

