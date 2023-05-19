using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ViewModel.Messages;

namespace ViewModel.Options;

public partial class YearOptions : ObservableObject
{
    public ObservableCollection<DropdownYearItems> Options { get; set; } = new();

    [RelayCommand]
    public void SelectAllYears()
    {
        foreach (var option in Options)
        {
            option.IsSelected = true;
        }
    }

    [RelayCommand]
    public void DeSelectAllYears()
    {
        foreach (var option in Options)
        {
            option.IsSelected = false;
        }
    }

    public void SetYears(IEnumerable<int> years)
    {
        Options.Clear();
        foreach (int year in years.OrderByDescending(i => i))
        {
            Options.Add(new DropdownYearItems() { Years = year, IsSelected = false });
        }
    }

    public List<int> GetSelectedYears() => Options.Where(i => i.IsSelected).Select(i => i.Years).ToList();
}

public partial class DropdownYearItems : ObservableRecipient
{
    public int Years { get; set; }
    private bool _isSelected;
    public bool IsSelected
    {
        get { return _isSelected; }
        set
        {
            SetProperty(ref _isSelected, value);
            Messenger.Send<YearSelectionChangedMessage>();
        }
    }
}

