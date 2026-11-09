using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CoachDesk.Desktop.Models.Anthropometry;

namespace CoachDesk.Desktop.ViewModels;

public sealed class PhotoEvidenceItemViewModel(PhotoEvidenceDto photo) : ViewModelBase
{
    public int Id { get; } = photo.Id;
    public string Type { get; } = photo.Type;
    public string FileName { get; } = photo.FileName;
    public long SizeBytes { get; } = photo.SizeBytes;
    private ImageSource? _preview;
    public ImageSource? Preview { get => _preview; private set => SetProperty(ref _preview, value); }
    public bool HasPreview => Preview is not null;

    public void SetPreview(byte[] content)
    {
        using var stream = new MemoryStream(content);
        var image = new BitmapImage();
        image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.StreamSource = stream; image.EndInit(); image.Freeze();
        Preview = image;
        OnPropertyChanged(nameof(HasPreview));
    }
}
