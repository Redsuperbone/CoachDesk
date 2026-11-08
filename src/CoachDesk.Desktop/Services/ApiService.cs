using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CoachDesk.Desktop.Models.Common;

namespace CoachDesk.Desktop.Services;

public sealed class ApiService : IApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IAuthenticationSession _session;

    public ApiService(HttpClient httpClient, IAuthenticationSession session)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public Task<TResponse> GetAsync<TResponse>(string relativeUri, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(HttpMethod.Get, relativeUri, null, cancellationToken);

    public Task<TResponse> PostAsync<TRequest, TResponse>(
        string relativeUri,
        TRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(HttpMethod.Post, relativeUri, request, cancellationToken);

    public async Task PutAsync<TRequest>(
        string relativeUri,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendRequestAsync(HttpMethod.Put, relativeUri, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task DeleteAsync(string relativeUri, CancellationToken cancellationToken = default)
    {
        using var response = await SendRequestAsync(HttpMethod.Delete, relativeUri, null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<TResponse> PostMultipartAsync<TResponse>(string relativeUri, IReadOnlyDictionary<string, string> fields, string fileFieldName, string fileName, string contentType, byte[] content, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, relativeUri);
        AddAuthorization(request);
        using var form = new MultipartFormDataContent();
        foreach (var field in fields) form.Add(new StringContent(field.Value), field.Key);
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, fileFieldName, fileName);
        request.Content = form;
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken) ?? throw new ApiException(response.StatusCode, "La API devolvió una respuesta vacía.");
    }

    private async Task<TResponse> SendAsync<TResponse>(
        HttpMethod method,
        string relativeUri,
        object? body,
        CancellationToken cancellationToken)
    {
        using var response = await SendRequestAsync(method, relativeUri, body, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);
        return result ?? throw new ApiException(response.StatusCode, "La API devolvió una respuesta vacía.");
    }

    private async Task<HttpResponseMessage> SendRequestAsync(
        HttpMethod method,
        string relativeUri,
        object? body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, relativeUri);
        AddAuthorization(request);

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private void AddAuthorization(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_session.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var error = TryReadError(content);
        var message = error?.Message ?? $"La API devolvió el estado {(int)response.StatusCode}.";

        throw new ApiException(response.StatusCode, message, error?.Code);
    }

    private static ApiErrorDto? TryReadError(string content)
    {
        try
        {
            return JsonSerializer.Deserialize<ApiErrorDto>(content, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
