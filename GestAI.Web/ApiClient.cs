using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        public ApiClientException(
            string message,
            HttpStatusCode statusCode,
            string? errorCode = null,
            IReadOnlyDictionary<string, string[]>? fieldErrors = null,
            string? correlationId = null)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            FieldErrors = fieldErrors;
            CorrelationId = correlationId;
        }

        public HttpStatusCode StatusCode { get; }
        public string? ErrorCode { get; }
        public IReadOnlyDictionary<string, string[]>? FieldErrors { get; }
        public string? CorrelationId { get; }
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

        var correlationFromHeader = TryReadCorrelationIdHeader(response);
        var payload = await response.Content.ReadAsStringAsync(ct);
        if (!string.IsNullOrWhiteSpace(payload))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<ApiErrorEnvelope>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed is not null && !string.IsNullOrWhiteSpace(parsed.Message))
                    throw new ApiClientException(
                        BuildUserMessage(parsed.Message!, parsed.ErrorCode, parsed.CorrelationId ?? correlationFromHeader),
                        response.StatusCode,
                        parsed.ErrorCode,
                        correlationId: parsed.CorrelationId ?? correlationFromHeader);

                var problem = JsonSerializer.Deserialize<ApiProblemDetails>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (problem is not null)
                {
                    var errorCode = problem.ErrorCode;
                    var correlationId = problem.CorrelationId ?? correlationFromHeader;

                    if (problem.Errors is { Count: > 0 })
                    {
                        var first = problem.Errors.FirstOrDefault(x => x.Value is { Length: > 0 });
                        if (first.Value is { Length: > 0 })
                            throw new ApiClientException(
                                BuildUserMessage(first.Value[0], errorCode ?? first.Key, correlationId),
                                response.StatusCode,
                                errorCode ?? first.Key,
                                problem.Errors,
                                correlationId);
                    }

                    if (!string.IsNullOrWhiteSpace(problem.Detail))
                        throw new ApiClientException(
                            BuildUserMessage(problem.Detail!, errorCode, correlationId),
                            response.StatusCode,
                            errorCode,
                            correlationId: correlationId);

                    if (!string.IsNullOrWhiteSpace(problem.Title))
                        throw new ApiClientException(
                            BuildUserMessage(problem.Title!, errorCode, correlationId),
                            response.StatusCode,
                            errorCode,
                            correlationId: correlationId);
                }
            }
            catch (JsonException)
            {
                var plain = payload.Trim();
                if (!string.IsNullOrWhiteSpace(plain))
                    throw new ApiClientException(
                        BuildUserMessage(plain.Length > 250 ? plain[..250] : plain, null, correlationFromHeader),
                        response.StatusCode,
                        correlationId: correlationFromHeader);
            }
        }

        throw new ApiClientException(
            BuildUserMessage($"Error HTTP {(int)response.StatusCode}.", null, correlationFromHeader),
            response.StatusCode,
            correlationId: correlationFromHeader);
    }

    private static string? TryReadCorrelationIdHeader(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("X-Correlation-ID", out var values))
            return null;

        var correlationId = values.FirstOrDefault()?.Trim();
        return string.IsNullOrWhiteSpace(correlationId) ? null : correlationId;
    }

    private static string BuildUserMessage(string message, string? errorCode, string? correlationId)
    {
        var suffixParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(errorCode))
            suffixParts.Add($"Código: {errorCode}");

        if (!string.IsNullOrWhiteSpace(correlationId))
            suffixParts.Add($"ID: {correlationId}");

        if (suffixParts.Count == 0)
            return message;

        return $"{message} ({string.Join(" · ", suffixParts)})";
    }

    private sealed record ApiErrorEnvelope(
        bool Success,
        [property: JsonPropertyName("errorCode")] string? ErrorCode,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("correlationId")] string? CorrelationId);

    private sealed record ApiProblemDetails(
        string? Title,
        string? Detail,
        Dictionary<string, string[]>? Errors,
        [property: JsonPropertyName("errorCode")] string? ErrorCode,
        [property: JsonPropertyName("correlationId")] string? CorrelationId);
}
