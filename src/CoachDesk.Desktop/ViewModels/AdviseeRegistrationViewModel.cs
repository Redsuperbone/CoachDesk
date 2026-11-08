using System.Net.Http;
using CoachDesk.Desktop.Commands;
using CoachDesk.Desktop.Models.Advisees;
using CoachDesk.Desktop.Services;

namespace CoachDesk.Desktop.ViewModels;

public sealed class AdviseeRegistrationViewModel : ViewModelBase
{
    private readonly IAdviseeService _adviseeService;
    private readonly Func<AdviseeSummaryDto, Task> _onCreated;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string? _sex;
    private DateTime? _birthDate;
    private string? _phoneNumber;
    private string? _occupation;
    private string? _goal;
    private DateTime? _coachingStartDate = DateTime.Today;
    private string? _message;
    private bool _isSaving;

    public AdviseeRegistrationViewModel(IAdviseeService adviseeService, Func<AdviseeSummaryDto, Task> onCreated)
    {
        _adviseeService = adviseeService;
        _onCreated = onCreated;
        SaveCommand = new AsyncRelayCommand(_ => SaveAsync(), _ => CanSave);
        ClearCommand = new RelayCommand(_ => Clear());
    }

    public string FirstName { get => _firstName; set { if (SetProperty(ref _firstName, value)) Refresh(); } }
    public string LastName { get => _lastName; set { if (SetProperty(ref _lastName, value)) Refresh(); } }
    public string? Sex { get => _sex; set => SetProperty(ref _sex, value); }
    public DateTime? BirthDate { get => _birthDate; set => SetProperty(ref _birthDate, value); }
    public string? PhoneNumber { get => _phoneNumber; set => SetProperty(ref _phoneNumber, value); }
    public string? Occupation { get => _occupation; set => SetProperty(ref _occupation, value); }
    public string? Goal { get => _goal; set => SetProperty(ref _goal, value); }
    public DateTime? CoachingStartDate { get => _coachingStartDate; set => SetProperty(ref _coachingStartDate, value); }
    public string? Message { get => _message; private set { if (SetProperty(ref _message, value)) OnPropertyChanged(nameof(HasMessage)); } }
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool IsSaving { get => _isSaving; private set { if (SetProperty(ref _isSaving, value)) Refresh(); } }
    public bool CanSave => !IsSaving && FirstName.Trim().Length >= 2 && !string.IsNullOrWhiteSpace(LastName);
    public AsyncRelayCommand SaveCommand { get; }
    public RelayCommand ClearCommand { get; }

    private async Task SaveAsync()
    {
        Message = null;
        IsSaving = true;
        try
        {
            var created = await _adviseeService.CreateAsync(new CreateAdviseeRequestDto
            {
                FirstName = FirstName, LastName = LastName, Sex = Sex, BirthDate = ToDateOnly(BirthDate), PhoneNumber = PhoneNumber,
                Occupation = Occupation, Goal = Goal, CoachingStartDate = ToDateOnly(CoachingStartDate)
            });
            await _onCreated(created);
            Clear();
        }
        catch (ApiException exception) { Message = exception.Message; }
        catch (HttpRequestException) { Message = "No fue posible registrar el asesorado. Revisa la conexión con el servidor."; }
        finally { IsSaving = false; }
    }

    private void Clear()
    {
        FirstName = string.Empty; LastName = string.Empty; Sex = null; BirthDate = null; PhoneNumber = null; Occupation = null; Goal = null; CoachingStartDate = DateTime.Today; Message = null;
    }

    private void Refresh()
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.RaiseCanExecuteChanged();
    }

    private static DateOnly? ToDateOnly(DateTime? value) => value is null ? null : DateOnly.FromDateTime(value.Value);
}
