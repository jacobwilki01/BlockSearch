using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BlockSearch.Avalonia.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanging, INotifyPropertyChanged
{
    public event PropertyChangingEventHandler? PropertyChanging;
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void NotifyPropertyChanging([CallerMemberName] string propertyName = "")
    {
        PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
    }
    
    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}