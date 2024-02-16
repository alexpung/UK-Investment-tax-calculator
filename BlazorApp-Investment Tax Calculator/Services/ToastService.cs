namespace Services;

public class ToastService
{
    public event Action<ToastOption>? ShowToastTrigger;
    public void ShowToast(string title, string content, ToastOptionType toastOptionType)
    {
        //Invoke ToastComponent to update and show the toast with messages  
        ShowToastTrigger?.Invoke(new ToastOption() { Title = title, Content = content, Type = toastOptionType });
    }

    public void ShowError(string content)
    {
        ShowToastTrigger?.Invoke(new ToastOption() { Title = "Error", Content = content, Type = ToastOptionType.Error });
    }

    public void ShowWarning(string content)
    {
        ShowToastTrigger?.Invoke(new ToastOption() { Title = "Warning", Content = content, Type = ToastOptionType.Warning });
    }

    public void ShowInformation(string content)
    {
        ShowToastTrigger?.Invoke(new ToastOption() { Title = "Infromation", Content = content, Type = ToastOptionType.Info });
    }

    public void ShowSuccess(string content)
    {
        ShowToastTrigger?.Invoke(new ToastOption() { Title = "Success", Content = content, Type = ToastOptionType.Success });
    }
}
public class ToastOption
{
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required ToastOptionType Type { get; set; }

}

public enum ToastOptionType
{
    Warning,
    Error,
    Success,
    Info
}
