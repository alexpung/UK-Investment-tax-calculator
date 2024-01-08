namespace Services;

public class ToastService
{
    public event Action<ToastOption>? ShowToastTrigger;
    public void ShowToast(string title, string content, ToastOptionType toastOptionType)
    {
        //Invoke ToastComponent to update and show the toast with messages  
        ShowToastTrigger?.Invoke(new ToastOption() { Title = title, Content = content, Type = toastOptionType });
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
