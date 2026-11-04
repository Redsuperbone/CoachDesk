using System.Text.RegularExpressions;
using System.Net.Http;
using CoachDesk.Desktop.Commands;
using CoachDesk.Desktop.Models.Authentication;
using CoachDesk.Desktop.Services;

namespace CoachDesk.Desktop.ViewModels;

public sealed class LoginViewModel : ViewModelBase
{
    private static readonly Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

    private readonly IAuthenticationService _authenticationService;
    private readonly Action _onLoginSucceeded;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;

    public LoginViewModel(IAuthenticationService authenticationService, Action onLoginSucceeded)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _onLoginSucceeded = onLoginSucceeded ?? throw new ArgumentNullException(nameof(onLoginSucceeded));
        LoginCommand = new AsyncRelayCommand(_ => LoginAsync(), _ => CanLogin);
    }

    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
            {
                ValidateInput();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                ValidateInput();
            }
        }
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

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool CanLogin =>
        !IsBusy &&
        !string.IsNullOrWhiteSpace(Password) &&
        EmailPattern.IsMatch(Email.Trim());

    public AsyncRelayCommand LoginCommand { get; }

    private void ValidateInput()
    {
        ErrorMessage = null;
        OnPropertyChanged(nameof(CanLogin));
        LoginCommand.RaiseCanExecuteChanged();
    }

    private async Task LoginAsync()
    {
        if (!CanLogin)
        {
            ErrorMessage = "Ingresa un correo válido y una contraseña.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        OnPropertyChanged(nameof(CanLogin));

        try
        {
            await _authenticationService.LoginAsync(
                new LoginRequestDto { Email = Email.Trim(), Password = Password });

            Password = string.Empty;
            _onLoginSucceeded();
        }
        catch (ApiException exception) when ((int)exception.StatusCode is 400 or 401)
        {
            ErrorMessage = "Correo o contraseña incorrectos.";
        }
        catch (ApiException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (UnauthorizedAccessException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "No fue posible conectar con el servidor. Intenta nuevamente.";
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = "La solicitud tardó demasiado. Intenta nuevamente.";
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanLogin));
            LoginCommand.RaiseCanExecuteChanged();
        }
    }
}
