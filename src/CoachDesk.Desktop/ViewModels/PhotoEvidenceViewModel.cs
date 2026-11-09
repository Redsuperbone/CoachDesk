using System.Collections.ObjectModel;
using CoachDesk.Desktop.Commands;
using CoachDesk.Desktop.Models.Anthropometry;
using CoachDesk.Desktop.Services;
using CoachDesk.Desktop.Views;
using System.Windows;

namespace CoachDesk.Desktop.ViewModels;

public sealed class PhotoEvidenceViewModel : ViewModelBase
{
    private readonly IPhotoEvidenceService _service;
    private int _adviseeId;
    private int _selectedMonth = 1;
    private string? _message;
    private PhotoEvidenceItemViewModel? _frontalPhoto;
    private PhotoEvidenceItemViewModel? _profilePhoto;
    private PhotoEvidenceItemViewModel? _backPhoto;

    public PhotoEvidenceViewModel(IPhotoEvidenceService service)
    {
        _service = service;
        OpenPreviewCommand = new RelayCommand(OpenPreview, value => value is PhotoEvidenceItemViewModel item && item.Preview is not null);
    }

    public ObservableCollection<PhotoEvidenceItemViewModel> Photos { get; } = [];
    public PhotoEvidenceItemViewModel? FrontalPhoto { get => _frontalPhoto; private set { if (SetProperty(ref _frontalPhoto, value)) OnPropertyChanged(nameof(HasFrontalPhoto)); } }
    public PhotoEvidenceItemViewModel? ProfilePhoto { get => _profilePhoto; private set { if (SetProperty(ref _profilePhoto, value)) OnPropertyChanged(nameof(HasProfilePhoto)); } }
    public PhotoEvidenceItemViewModel? BackPhoto { get => _backPhoto; private set { if (SetProperty(ref _backPhoto, value)) OnPropertyChanged(nameof(HasBackPhoto)); } }
    public bool HasFrontalPhoto => FrontalPhoto is not null;
    public bool HasProfilePhoto => ProfilePhoto is not null;
    public bool HasBackPhoto => BackPhoto is not null;
    public IEnumerable<int> Months => Enumerable.Range(1, 10);
    public int SelectedMonth
    {
        get => _selectedMonth;
        set
        {
            if (SetProperty(ref _selectedMonth, value))
            {
                OnPropertyChanged(nameof(SelectedMonthLabel));
                _ = LoadAsync();
            }
        }
    }
    public string SelectedMonthLabel => SelectedMonth == 0 ? "Inicio" : $"Mes {SelectedMonth}";
    public string? Message { get => _message; private set { if (SetProperty(ref _message, value)) OnPropertyChanged(nameof(HasMessage)); } }
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public RelayCommand OpenPreviewCommand { get; }

    public async Task LoadForAsync(int adviseeId)
    {
        _adviseeId = adviseeId;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_adviseeId <= 0) return;
        try
        {
            var photos = await _service.GetAsync(_adviseeId);
            Photos.Clear();
            foreach (var photo in photos.Where(photo => photo.Month == SelectedMonth))
            {
                var item = new PhotoEvidenceItemViewModel(photo);
                Photos.Add(item);
                try { item.SetPreview(await _service.GetContentAsync(_adviseeId, item.Id)); }
                catch (Exception) { /* El nombre sigue visible si no se puede descargar la miniatura. */ }
            }
            FrontalPhoto = Photos.FirstOrDefault(item => item.Type == "Frontal");
            ProfilePhoto = Photos.FirstOrDefault(item => item.Type == "Perfil");
            BackPhoto = Photos.FirstOrDefault(item => item.Type == "Espalda");
            OpenPreviewCommand.RaiseCanExecuteChanged();
        }
        catch (Exception) { Message = "No fue posible cargar las evidencias fotográficas."; }
    }

    private static void OpenPreview(object? value)
    {
        if (value is not PhotoEvidenceItemViewModel item || item.Preview is null) return;
        var window = new PhotoPreviewWindow(item.Preview, $"{item.Type} · {item.FileName}") { Owner = Application.Current?.MainWindow };
        window.ShowDialog();
    }

}
