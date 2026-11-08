using System.Collections.ObjectModel;
using System.Net.Http;
using CoachDesk.Desktop.Commands;
using CoachDesk.Desktop.Models.Advisees;
using CoachDesk.Desktop.Models.Anthropometry;
using CoachDesk.Desktop.Services;

namespace CoachDesk.Desktop.ViewModels;

public sealed class AnthropometricRecordViewModel : ViewModelBase
{
    private readonly IAnthropometricService _anthropometricService;
    private AdviseeSummaryDto? _advisee;
    private bool _isLoading;
    private bool _isSaving;
    private string? _errorMessage;
    private string? _successMessage;

    public AnthropometricRecordViewModel(IAnthropometricService anthropometricService, IPhotoEvidenceService photoEvidenceService)
    {
        _anthropometricService = anthropometricService ?? throw new ArgumentNullException(nameof(anthropometricService));
        Rows = new ObservableCollection<AnthropometricMetricRowViewModel>(CreateRows());
        Photos = new PhotoEvidenceViewModel(photoEvidenceService);
        SaveCommand = new AsyncRelayCommand(_ => SaveAsync(), _ => CanSave);
    }

    public ObservableCollection<AnthropometricMetricRowViewModel> Rows { get; }
    public PhotoEvidenceViewModel Photos { get; }
    public AdviseeSummaryDto? Advisee { get => _advisee; private set => SetProperty(ref _advisee, value); }
    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }
    public bool IsSaving { get => _isSaving; private set => SetProperty(ref _isSaving, value); }
    public string? ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }
    public string? SuccessMessage { get => _successMessage; private set => SetProperty(ref _successMessage, value); }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);
    public bool CanSave => Advisee is not null && !IsLoading && !IsSaving && Rows.All(row => !row.HasErrors);
    public AsyncRelayCommand SaveCommand { get; }

    public async Task LoadForAsync(AdviseeSummaryDto advisee)
    {
        Advisee = advisee;
        ErrorMessage = null;
        SuccessMessage = null;
        IsLoading = true;
        NotifyStateChanged();

        try
        {
            var profile = await _anthropometricService.GetProfileAsync(advisee.Id);
            await Photos.LoadForAsync(advisee.Id);
            ClearRows();
            foreach (var assessment in profile.Assessments.Where(item => item.MonthNumber is >= 1 and <= 10))
            {
                SetMonth(assessment);
            }
        }
        catch (ApiException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "No fue posible cargar el expediente antropométrico.";
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = "La carga del expediente tardó demasiado.";
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    private async Task SaveAsync()
    {
        if (!CanSave || Advisee is null)
        {
            ErrorMessage = "Revisa los valores fuera de rango antes de guardar.";
            return;
        }

        IsSaving = true;
        ErrorMessage = null;
        SuccessMessage = null;
        NotifyStateChanged();

        try
        {
            await _anthropometricService.SaveProfileAsync(BuildProfile());
            SuccessMessage = "Expediente antropométrico guardado correctamente.";
        }
        catch (ApiException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "No fue posible guardar los cambios. Intenta nuevamente.";
        }
        finally
        {
            IsSaving = false;
            NotifyStateChanged();
        }
    }

    private AnthropometricProfileDto BuildProfile() => new()
    {
        AdviseeId = Advisee!.Id,
        Assessments = Enumerable.Range(1, 10).Select(BuildAssessment).ToArray()
    };

    private AnthropometricAssessmentDto BuildAssessment(int month) => new()
    {
        MonthNumber = month,
        WeightKg = GetRow("weight").GetValue(month),
        HeightCm = GetRow("height").GetValue(month),
        BicipitalSkinfoldMm = GetRow("bicipital").GetValue(month),
        TricipitalSkinfoldMm = GetRow("tricipital").GetValue(month),
        SubscapularSkinfoldMm = GetRow("subscapular").GetValue(month),
        SuprailiacSkinfoldMm = GetRow("suprailiac").GetValue(month),
        WaistCm = GetRow("waist").GetValue(month),
        HipCm = GetRow("hip").GetValue(month),
        ArmCm = GetRow("arm").GetValue(month),
        ThighCm = GetRow("thigh").GetValue(month)
    };

    private void SetMonth(AnthropometricAssessmentDto value)
    {
        var month = value.MonthNumber;
        GetRow("weight").SetValue(month, value.WeightKg);
        GetRow("height").SetValue(month, value.HeightCm);
        GetRow("bicipital").SetValue(month, value.BicipitalSkinfoldMm);
        GetRow("tricipital").SetValue(month, value.TricipitalSkinfoldMm);
        GetRow("subscapular").SetValue(month, value.SubscapularSkinfoldMm);
        GetRow("suprailiac").SetValue(month, value.SuprailiacSkinfoldMm);
        GetRow("waist").SetValue(month, value.WaistCm);
        GetRow("hip").SetValue(month, value.HipCm);
        GetRow("arm").SetValue(month, value.ArmCm);
        GetRow("thigh").SetValue(month, value.ThighCm);
    }

    private void ClearRows()
    {
        foreach (var row in Rows)
        {
            for (var month = 1; month <= 10; month++) row.SetValue(month, null);
        }
    }

    private AnthropometricMetricRowViewModel GetRow(string key) => Rows.Single(row => row.Key == key);

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasSuccess));
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.RaiseCanExecuteChanged();
    }

    private static IEnumerable<AnthropometricMetricRowViewModel> CreateRows()
    {
        yield return new("weight", "Peso", "kg", 25, 350);
        yield return new("height", "Estatura", "cm", 100, 250);
        yield return new("bicipital", "Pliegue bicipital", "mm", 1, 100);
        yield return new("tricipital", "Pliegue tricipital", "mm", 1, 100);
        yield return new("subscapular", "Pliegue subescapular", "mm", 1, 100);
        yield return new("suprailiac", "Pliegue suprailíaco", "mm", 1, 100);
        yield return new("waist", "Perímetro cintura", "cm", 20, 200);
        yield return new("hip", "Perímetro cadera", "cm", 20, 220);
        yield return new("arm", "Perímetro brazo", "cm", 10, 100);
        yield return new("thigh", "Perímetro muslo", "cm", 15, 150);
    }
}
