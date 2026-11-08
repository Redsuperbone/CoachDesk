using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using Microsoft.Win32;
using CoachDesk.Desktop.Commands;
using CoachDesk.Desktop.Models.Anthropometry;
using CoachDesk.Desktop.Services;

namespace CoachDesk.Desktop.ViewModels;

public sealed class PhotoEvidenceViewModel : ViewModelBase
{
    private readonly IPhotoEvidenceService _service;
    private int _adviseeId;
    private int _selectedPeriod = 1;
    private int _selectedMonth = 1;
    private string? _message;
    private bool _isUploading;

    public PhotoEvidenceViewModel(IPhotoEvidenceService service)
    {
        _service = service;
        UploadCommand = new AsyncRelayCommand(UploadAsync, _ => _adviseeId > 0 && !IsUploading);
    }

    public ObservableCollection<PhotoEvidenceDto> Photos { get; } = [];
    public int SelectedPeriod { get => _selectedPeriod; set { if (SetProperty(ref _selectedPeriod, value)) _ = LoadAsync(); } }
    public IEnumerable<int> Months => Enumerable.Range(0, 11);
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
    public bool IsUploading { get => _isUploading; private set { if (SetProperty(ref _isUploading, value)) UploadCommand.RaiseCanExecuteChanged(); } }
    public AsyncRelayCommand UploadCommand { get; }

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
            foreach (var photo in photos.Where(photo => photo.Period == SelectedPeriod && photo.Month == SelectedMonth)) Photos.Add(photo);
        }
        catch (Exception) { Message = "No fue posible cargar las evidencias fotográficas."; }
    }

    private async Task UploadAsync(object? parameter)
    {
        var type = parameter as string;
        if (type is not ("Frontal" or "Perfil" or "Espalda")) return;
        var dialog = new OpenFileDialog { Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.webp", Multiselect = false };
        if (dialog.ShowDialog() != true) return;
        var info = new FileInfo(dialog.FileName);
        if (info.Length > 1_572_864) { Message = "La imagen supera el límite de 1.5 MB."; return; }
        var contentType = Path.GetExtension(info.Name).ToLowerInvariant() switch { ".png" => "image/png", ".webp" => "image/webp", _ => "image/jpeg" };
        IsUploading = true; Message = null;
        try
        {
            await _service.UploadAsync(_adviseeId, SelectedPeriod, SelectedMonth, type, info.Name, contentType, await File.ReadAllBytesAsync(info.FullName));
            await LoadAsync();
            Message = $"Fotografía {type.ToLowerInvariant()} guardada para el mes {SelectedMonth}.";
        }
        catch (ApiException exception) { Message = exception.Message; }
        catch (HttpRequestException) { Message = "No fue posible subir la fotografía."; }
        finally { IsUploading = false; }
    }
}
