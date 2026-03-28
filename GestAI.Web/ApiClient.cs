using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GestAI.Web;

public sealed class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public bool IsBusy { get; private set; }
    public event Action<bool>? OnBusyChanged;
    private void SetBusy(bool v) { IsBusy = v; OnBusyChanged?.Invoke(v); }

    public event Action<string>? OnToast;
    public void Toast(string message) => OnToast?.Invoke(message);

    public sealed class ApiClientException : Exception
    {
        public ApiClientException(string message, HttpStatusCode statusCode, string? errorCode = null, IReadOnlyDictionary<string, string[]>? fieldErrors = null)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            FieldErrors = fieldErrors;
        }

        public HttpStatusCode StatusCode { get; }
        public string? ErrorCode { get; }
        public IReadOnlyDictionary<string, string[]>? FieldErrors { get; }
    }

    private static string Normalize(string url)
        => (url ?? string.Empty).TrimStart('/');

    public async Task<T?> GetAsync<T>(string url, CancellationToken ct = default)
    {
        try
        {
            SetBusy(true);
            using var res = await _http.GetAsync(Normalize(url), ct);
            await EnsureSuccessOrThrowAsync(res, ct);
            return await res.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        }
        finally
        {
            SetBusy(false);
        }
    }

    public async Task<byte[]> GetBytesAsync(string url, CancellationToken ct = default)
    {
        try
        {
            SetBusy(true);
            using var res = await _http.GetAsync(Normalize(url), ct);
            await EnsureSuccessOrThrowAsync(res, ct);
            return await res.Content.ReadAsByteArrayAsync(ct);
        }
        finally
        {
            SetBusy(false);
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default)
    {
        try
        {
            SetBusy(true);
            using var res = await _http.PostAsJsonAsync(Normalize(url), body, ct);
            await EnsureSuccessOrThrowAsync(res, ct);

            if (res.StatusCode == HttpStatusCode.NoContent)
                return default;

            return await res.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
        }
        finally
        {
            SetBusy(false);
        }
    }

    public Task<TResponse?> PostAsync<TResponse>(string url, object? body, CancellationToken ct = default)
        => PostAsync<object?, TResponse>(url, body, ct);

    public async Task PostAsync<TRequest>(string url, TRequest body, CancellationToken ct = default)
    {
        try
        {
            SetBusy(true);
            using var res = await _http.PostAsJsonAsync(Normalize(url), body, ct);
            await EnsureSuccessOrThrowAsync(res, ct);
        }
        finally
        {
            SetBusy(false);
        }
    }

    public Task<TResponse?> PostJsonAsync<TResponse, TRequest>(string url, TRequest body, CancellationToken ct = default)
        => PostAsync<TRequest, TResponse>(url, body, ct);

    public async Task PutAsync<TRequest>(string url, TRequest body, CancellationToken ct = default)
    {
        try
        {
            SetBusy(true);
            using var res = await _http.PutAsJsonAsync(Normalize(url), body, ct);
            await EnsureSuccessOrThrowAsync(res, ct);
        }
        finally
        {
            SetBusy(false);
        }
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default)
    {
        try
        {
            SetBusy(true);
            using var res = await _http.PutAsJsonAsync(Normalize(url), body, ct);
            await EnsureSuccessOrThrowAsync(res, ct);
            return await res.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
        }
        finally
        {
            SetBusy(false);
        }
    }

    public async Task DeleteAsync(string url, CancellationToken ct = default)
    {
        try
        {
            SetBusy(true);
            using var res = await _http.DeleteAsync(Normalize(url), ct);
            await EnsureSuccessOrThrowAsync(res, ct);
        }
        finally
        {
            SetBusy(false);
        }
    }

    public async Task<TResponse?> DeleteAsync<TResponse>(string url, CancellationToken ct = default)
    {
        try
        {
            SetBusy(true);
            using var res = await _http.DeleteAsync(Normalize(url), ct);
            await EnsureSuccessOrThrowAsync(res, ct);

            if (res.StatusCode == HttpStatusCode.NoContent)
                return default;

            return await res.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var payload = await response.Content.ReadAsStringAsync(ct);
        if (!string.IsNullOrWhiteSpace(payload))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<ApiErrorEnvelope>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed is not null && !string.IsNullOrWhiteSpace(parsed.Message))
                    throw new ApiClientException(parsed.Message!, response.StatusCode, parsed.ErrorCode);

                var problem = JsonSerializer.Deserialize<ApiProblemDetails>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (problem is not null)
                {
                    if (problem.Errors is { Count: > 0 })
                    {
                        var first = problem.Errors.FirstOrDefault(x => x.Value is { Length: > 0 });
                        if (first.Value is { Length: > 0 })
                            throw new ApiClientException(first.Value[0], response.StatusCode, first.Key, problem.Errors);
                    }

                    if (!string.IsNullOrWhiteSpace(problem.Detail))
                        throw new ApiClientException(problem.Detail!, response.StatusCode);

                    if (!string.IsNullOrWhiteSpace(problem.Title))
                        throw new ApiClientException(problem.Title!, response.StatusCode);
                }
            }
            catch (JsonException)
            {
                var plain = payload.Trim();
                if (!string.IsNullOrWhiteSpace(plain))
                    throw new ApiClientException(plain.Length > 250 ? plain[..250] : plain, response.StatusCode);
            }
        }

        throw new ApiClientException($"Error HTTP {(int)response.StatusCode}.", response.StatusCode);
    }

    private sealed record ApiErrorEnvelope(bool Success, string? ErrorCode, string? Message);
    private sealed record ApiProblemDetails(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}
