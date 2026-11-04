using System.Windows;
using CoachDesk.Desktop.ViewModels;

namespace CoachDesk.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
