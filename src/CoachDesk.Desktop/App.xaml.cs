using System.Net.Http;
using System.Windows;
using CoachDesk.Desktop.Configuration;
using CoachDesk.Desktop.Services;
using CoachDesk.Desktop.ViewModels;
using CoachDesk.Desktop.Views;

namespace CoachDesk.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var options = new ApiOptions();
        var session = new AuthenticationSession();
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(options.BaseUrl),
            Timeout = options.Timeout
        };

        var apiService = new ApiService(httpClient, session);
        var authenticationService = new AuthenticationService(apiService, session);
        var adviseeService = new AdviseeService(apiService);
        var anthropometricService = new AnthropometricService(apiService);
        var nutritionService = new NutritionService(apiService);
        var appointmentService = new AppointmentService(apiService);
        var pdfExporter = new MealPlanPdfExporter();
        var fileDialogService = new FileDialogService();

        LoginWindow? loginWindow = null;
        loginWindow = new LoginWindow
        {
            DataContext = new LoginViewModel(authenticationService, () =>
            {
                var mainViewModel = new MainViewModel(
                    session,
                    adviseeService,
                    anthropometricService,
                    nutritionService,
                    appointmentService,
                    pdfExporter,
                    fileDialogService);
                var mainWindow = new MainWindow(mainViewModel);
                MainWindow = mainWindow;
                mainWindow.Show();
                _ = mainViewModel.InitializeAsync();
                loginWindow?.Close();
            })
        };

        MainWindow = loginWindow;
        loginWindow.Show();
    }
}
