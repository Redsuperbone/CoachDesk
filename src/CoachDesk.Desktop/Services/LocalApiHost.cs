using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace CoachDesk.Desktop.Services;

/// <summary>
/// Inicia la API local incluida con la distribución de escritorio y la detiene al cerrar CoachDesk.
/// No toma control de una API que ya estuviera ejecutándose en localhost.
/// </summary>
public sealed class LocalApiHost : IDisposable
{
    private const string ApiBaseUrl = "http://localhost:5001/";
    private Process? _startedProcess;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (await IsAvailableAsync(cancellationToken)) return;

        var apiExecutable = FindApiExecutable();
        if (apiExecutable is null)
        {
            throw new FileNotFoundException(
                "No se encontró la API local. Publica CoachDesk con scripts\\Publish-Standalone.ps1 para crear la carpeta Api.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = apiExecutable,
            WorkingDirectory = Path.GetDirectoryName(apiExecutable)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["ASPNETCORE_URLS"] = ApiBaseUrl;

        _startedProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException("No fue posible iniciar la API local.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        while (!timeout.IsCancellationRequested)
        {
            if (_startedProcess.HasExited)
            {
                throw new InvalidOperationException("La API local se cerró durante el inicio. Revisa su archivo de configuración.");
            }

            if (await IsAvailableAsync(timeout.Token)) return;
            await Task.Delay(350, timeout.Token);
        }

        throw new TimeoutException("La API local no respondió en http://localhost:5001.");
    }

    public void Dispose()
    {
        if (_startedProcess is { HasExited: false })
        {
            _startedProcess.Kill(entireProcessTree: true);
            _startedProcess.WaitForExit(3000);
        }

        _startedProcess?.Dispose();
    }

    private static async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
            using var response = await client.GetAsync("http://localhost:5001/swagger/index.html", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException) { return false; }
        catch (TaskCanceledException) { return false; }
    }

    private static string? FindApiExecutable()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDirectory, "Api", "CoachDesk.Api.exe"),
            Path.Combine(baseDirectory, "CoachDesk.Api.exe"),
            Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", "CoachDesk.Api", "bin", "Debug", "net8.0", "CoachDesk.Api.exe")),
            Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", "CoachDesk.Api", "bin", "Release", "net8.0", "CoachDesk.Api.exe"))
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
