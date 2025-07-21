using Avalonia.Controls;
using BlockSearch.Avalonia.ViewModels;

namespace BlockSearch.Avalonia.Views;

public partial class MainWindow : Window
{
    /// <summary>
    /// The view model for the main window.
    /// </summary>
    public MainWindowViewModel ViewModel { get; } = new();
    
    public MainWindow()
    {
        DataContext = ViewModel;
        
        InitializeComponent();
    }
}