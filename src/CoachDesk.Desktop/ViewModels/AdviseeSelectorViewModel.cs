using System.Collections.ObjectModel;
using System.Net.Http;
using CoachDesk.Desktop.Commands;
using CoachDesk.Desktop.Models.Advisees;
using CoachDesk.Desktop.Services;

namespace CoachDesk.Desktop.ViewModels;

public sealed class AdviseeSelectorViewModel : ViewModelBase
{
    private readonly IAdviseeService _adviseeService;
    private CancellationTokenSource? _searchCancellationTokenSource;
    private string _searchTerm = string.Empty;
    private AdviseeSummaryDto? _selectedAdvisee;
    private string? _errorMessage;
    private bool _isLoading;

    public AdviseeSelectorViewModel(IAdviseeService adviseeService)
    {
        _adviseeService = adviseeService ?? throw new ArgumentNullException(nameof(adviseeService));
        ReloadCommand = new AsyncRelayCommand(_ => LoadAsync(), _ => !IsLoading);
    }

    public ObservableCollection<AdviseeSummaryDto> Advisees { get; } = [];
    public int ActiveCount => Advisees.Count;

    public string SearchTerm
    {
        get => _searchTerm;
        set
        {
            if (SetProperty(ref _searchTerm, value))
            {
                StartSearch();
            }
        }
    }

    public AdviseeSummaryDto? SelectedAdvisee
    {
        get => _selectedAdvisee;
        set => SetProperty(ref _selectedAdvisee, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                ReloadCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public AsyncRelayCommand ReloadCommand { get; }

    public Task LoadAsync() => LoadAsync(CancellationToken.None);

    private void StartSearch()
    {
        _searchCancellationTokenSource?.Cancel();
        _searchCancellationTokenSource?.Dispose();
        _searchCancellationTokenSource = new CancellationTokenSource();
        _ = LoadAfterDelayAsync(_searchCancellationTokenSource.Token);
    }

    private async Task LoadAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
            await LoadAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var selectedId = SelectedAdvisee?.Id;
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var advisees = await _adviseeService.GetActiveAsync(SearchTerm, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Advisees.Clear();
            foreach (var advisee in advisees)
            {
                Advisees.Add(advisee);
            }
            OnPropertyChanged(nameof(ActiveCount));

            SelectedAdvisee = Advisees.FirstOrDefault(item => item.Id == selectedId) ?? Advisees.FirstOrDefault();
        }
        catch (OperationCanceledException)
        {
        }
        catch (ApiException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "No fue posible cargar los asesorados. Revisa la conexión con el servidor.";
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                IsLoading = false;
            }
        }
    }
}
