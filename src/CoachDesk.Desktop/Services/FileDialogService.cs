using Microsoft.Win32;

namespace CoachDesk.Desktop.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? GetPdfSavePath(string suggestedFileName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Documento PDF (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            AddExtension = true,
            FileName = suggestedFileName
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
