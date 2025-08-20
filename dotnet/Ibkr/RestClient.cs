using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Ibkr;

public record Result<T>(T? Data, HttpRequestMessage Request);

public class RestClient : IDisposable
{
    private readonly HttpClient _httpClient;
    public string BaseUrl { get; }

    public RestClient(string baseUrl, HttpMessageHandler? handler = null)
    {
        if (string.IsNullOrEmpty(baseUrl))
            throw new ArgumentException("Base URL must not be null", nameof(baseUrl));
        BaseUrl = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
        _httpClient = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
    }

    protected virtual Task<Dictionary<string, string>> GetHeadersAsync(string method, string url)
    {
        return Task.FromResult(new Dictionary<string, string>());
    }

    private static string BuildUrl(string baseUrl, string endpoint, Dictionary<string, string>? query)
    {
        var url = baseUrl + endpoint.TrimStart('/');
        if (query != null && query.Count > 0)
        {
            var q = string.Join("&", query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
            url += "?" + q;
        }
        return url;
    }

    protected async Task<Result<JsonDocument>> SendAsync(HttpMethod method, string endpoint, Dictionary<string, string>? query = null, object? body = null, Dictionary<string,string>? extraHeaders = null)
    {
        var url = BuildUrl(BaseUrl, endpoint, query);
        var request = new HttpRequestMessage(method, url);
        if (body != null)
        {
            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        var headers = await GetHeadersAsync(method.Method, url);
        foreach (var kv in headers)
            request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
        if (extraHeaders != null)
        {
            foreach (var kv in extraHeaders)
                request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
        }

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);
        return new Result<JsonDocument>(doc, request);
    }

    public Task<Result<JsonDocument>> GetAsync(string endpoint, Dictionary<string, string>? query = null, Dictionary<string,string>? extraHeaders=null)
        => SendAsync(HttpMethod.Get, endpoint, query, null, extraHeaders);

    public Task<Result<JsonDocument>> PostAsync(string endpoint, object? body = null, Dictionary<string, string>? query = null, Dictionary<string,string>? extraHeaders=null)
        => SendAsync(HttpMethod.Post, endpoint, query, body, extraHeaders);

    public Task<Result<JsonDocument>> DeleteAsync(string endpoint, object? body = null, Dictionary<string, string>? query = null, Dictionary<string,string>? extraHeaders=null)
        => SendAsync(HttpMethod.Delete, endpoint, query, body, extraHeaders);

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
