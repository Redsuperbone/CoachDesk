using System.Windows;

namespace CoachDesk.Desktop.Services;

public sealed class ConfirmationDialogService : IConfirmationDialogService
{
    public bool Confirm(string message, string title) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
}
