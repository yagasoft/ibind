using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Net;
using Helpers;

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

		if (handler is HttpClientHandler clientHandler)
		{
			// Ensure decompression is enabled if caller gave a HttpClientHandler
			clientHandler.AutomaticDecompression =
				DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli;

			_httpClient = new HttpClient(clientHandler, disposeHandler: false);
		}
		else if (handler is not null)
		{
			_httpClient = new HttpClient(handler, disposeHandler: false);
		}
		else
		{
			var newHandler = new HttpClientHandler
							 {
								 AutomaticDecompression =
									 DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
							 };

			_httpClient = new HttpClient(newHandler, disposeHandler: true);
		}
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

    private static string BuildUrl(string baseUrl, string endpoint, Dictionary<string, IReadOnlyList<string>>? query)
    {
        var url = baseUrl + endpoint.TrimStart('/');
        if (query != null && query.Count > 0)
        {
            var q = string.Join("&", query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={kv.Value.Select(Uri.EscapeDataString).StringAggregate()}"));
            url += "?" + q;
        }
        return url;
    }

	protected async Task<Result<JsonDocument>> SendAsync(
		HttpMethod method,
		string endpoint,
		Dictionary<string, string>? query = null,
		object? body = null,
		Dictionary<string, string>? extraHeaders = null)
	{
		return await SendAsync(method, endpoint, query?.ToDictionary(k => k.Key, k => (IReadOnlyList<string>)[ k.Value ]), body, extraHeaders);
	}

	protected async Task<Result<JsonDocument>> SendAsync(
		HttpMethod method,
		string endpoint,
		Dictionary<string, IReadOnlyList<string>>? query = null,
		object? body = null,
		Dictionary<string, string>? extraHeaders = null)
	{
		var url = BuildUrl(BaseUrl, endpoint, query);
		var request = new HttpRequestMessage(method, url);

		// Add body if present
		if (body != null)
		{
			var json = JsonSerializer.Serialize(
				body,
				new JsonSerializerOptions
				{
					DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
				});
			request.Content = new StringContent(json, Encoding.UTF8, "application/json");
		}

		// Add headers
		var headers = await GetHeadersAsync(method.Method, url);
		foreach (var kv in headers)
			request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);

		if (extraHeaders != null)
		{
			foreach (var kv in extraHeaders)
				request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
		}

		// Send request
		var response = await _httpClient.SendAsync(request);
		// Read content as string so we can log and parse
		var responseText = await response.Content.ReadAsStringAsync();

		response.EnsureSuccessStatusCode();

		// Print to console
		Console.WriteLine("Response:");
		Console.WriteLine(responseText);

		// Parse into JsonDocument
		using var doc = JsonDocument.Parse(responseText);

		// Clone the document so we can return it safely outside using
		var resultDoc = JsonDocument.Parse(responseText);

		return new Result<JsonDocument>(resultDoc, request);
	}

    public Task<Result<JsonDocument>> GetAsync(string endpoint, Dictionary<string, string>? query = null, Dictionary<string,string>? extraHeaders=null)
        => SendAsync(HttpMethod.Get, endpoint, query, null, extraHeaders);

    public Task<Result<JsonDocument>> GetAsync(string endpoint, Dictionary<string, IReadOnlyList<string>>? query, Dictionary<string,string>? extraHeaders=null)
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
