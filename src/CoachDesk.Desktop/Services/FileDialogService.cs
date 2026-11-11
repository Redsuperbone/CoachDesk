using Microsoft.Win32;
using System.Windows;

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

        return dialog.ShowDialog(Application.Current?.MainWindow) == true ? dialog.FileName : null;
    }
}
