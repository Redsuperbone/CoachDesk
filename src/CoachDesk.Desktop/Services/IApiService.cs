namespace CoachDesk.Desktop.Services;

public interface IApiService
{
    Task<TResponse> GetAsync<TResponse>(string relativeUri, CancellationToken cancellationToken = default);

    Task<TResponse> PostAsync<TRequest, TResponse>(
        string relativeUri,
        TRequest request,
        CancellationToken cancellationToken = default);

    Task PutAsync<TRequest>(
        string relativeUri,
        TRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string relativeUri, CancellationToken cancellationToken = default);
}
