namespace CoachDesk.Desktop.Services;

public interface IFileDialogService
{
    string? GetPdfSavePath(string suggestedFileName);
}
