using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using CoachDesk.Api.Configuration;
using Microsoft.Extensions.Options;

namespace CoachDesk.Api.Services;

/// <summary>
/// Cliente de servidor para Supabase Auth y PostgREST. Cada operación de datos
/// reenvía el JWT del usuario, por lo que las políticas RLS de Supabase siguen
/// siendo la autoridad sobre las filas que se pueden consultar o modificar.
/// </summary>
public sealed class SupabaseClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly SupabaseOptions _options;

    public SupabaseClient(HttpClient httpClient, IOptions<SupabaseOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.Url) || string.IsNullOrWhiteSpace(_options.PublishableKey))
        {
            throw new InvalidOperationException("Configura Supabase:Url y Supabase:PublishableKey mediante User Secrets o variables de entorno.");
        }

        _httpClient.BaseAddress = new Uri(_options.Url.TrimEnd('/') + "/");
    }

    public async Task<SupabaseSession?> SignInAsync(string email, string password, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, "auth/v1/token?grant_type=password", null);
        request.Content = JsonContent.Create(new { email, password });
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<JsonObject>(JsonOptions, cancellationToken);
        var accessToken = payload?["access_token"]?.GetValue<string>();
        var userId = payload?["user"]?["id"]?.GetValue<string>();
        var userEmail = payload?["user"]?["email"]?.GetValue<string>();
        var expiresIn = payload?["expires_in"]?.GetValue<int?>();
        return string.IsNullOrWhiteSpace(accessToken) || !Guid.TryParse(userId, out var parsedUserId)
            ? null
            : new SupabaseSession(accessToken, parsedUserId, userEmail ?? email, expiresIn);
    }

    public async Task<SupabaseUser?> GetUserAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, "auth/v1/user", accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        var user = await response.Content.ReadFromJsonAsync<JsonObject>(JsonOptions, cancellationToken);
        return Guid.TryParse(user?["id"]?.GetValue<string>(), out var userId)
            ? new SupabaseUser(userId, user?["email"]?.GetValue<string>() ?? string.Empty)
            : null;
    }

    public async Task<JsonArray> GetRowsAsync(string relativeUri, string accessToken, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, "rest/v1/" + relativeUri, accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<JsonArray>(JsonOptions, cancellationToken) ?? [];
    }

    public async Task<JsonArray> PostRowsAsync(string relativeUri, JsonNode body, string accessToken, string prefer, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, "rest/v1/" + relativeUri, accessToken);
        request.Headers.TryAddWithoutValidation("Prefer", prefer);
        request.Content = JsonContent.Create(body);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        // PostgREST responde correctamente sin cuerpo cuando se solicita return=minimal.
        // No se debe intentar deserializar una respuesta vacía como JSON.
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(content)
            ? []
            : JsonSerializer.Deserialize<JsonArray>(content, JsonOptions) ?? [];
    }

    public async Task PatchAsync(string relativeUri, JsonNode body, string accessToken, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Patch, "rest/v1/" + relativeUri, accessToken);
        request.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
        request.Content = JsonContent.Create(body);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task DeleteRowsAsync(string relativeUri, string accessToken, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Delete, "rest/v1/" + relativeUri, accessToken);
        request.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<JsonNode?> RpcAsync(string functionName, JsonNode body, string accessToken, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, "rest/v1/rpc/" + functionName, accessToken);
        request.Content = JsonContent.Create(body);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<JsonNode>(JsonOptions, cancellationToken);
    }

    public async Task UploadStorageObjectAsync(string bucket, string path, Stream content, string contentType, string accessToken, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, $"storage/v1/object/{bucket}/{Uri.EscapeDataString(path).Replace("%2F", "/")}", accessToken);
        request.Headers.TryAddWithoutValidation("x-upsert", "true");
        request.Content = new StreamContent(content);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<(byte[] Content, string ContentType)> DownloadStorageObjectAsync(string bucket, string path, string accessToken, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, $"storage/v1/object/authenticated/{bucket}/{Uri.EscapeDataString(path).Replace("%2F", "/")}", accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadAsByteArrayAsync(cancellationToken), response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUri, string? accessToken)
    {
        var request = new HttpRequestMessage(method, relativeUri);
        request.Headers.TryAddWithoutValidation("apikey", _options.PublishableKey);
        if (!string.IsNullOrWhiteSpace(accessToken)) request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + accessToken);
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new SupabaseRequestException(response.StatusCode, string.IsNullOrWhiteSpace(detail) ? "Supabase rechazó la operación." : detail);
    }
}

public sealed record SupabaseSession(string AccessToken, Guid UserId, string Email, int? ExpiresIn);
public sealed record SupabaseUser(Guid Id, string Email);

public sealed class SupabaseRequestException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
