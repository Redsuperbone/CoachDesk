namespace CoachDesk.Desktop.Services;

public interface IConfirmationDialogService
{
    bool Confirm(string message, string title);
}
