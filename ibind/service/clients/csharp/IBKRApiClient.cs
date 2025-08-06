// IBKRApiClient.cs - C# .NET 9 client for IBKR Web Service
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace IBKRClient;

public record HealthResponse(
    string Status,
    bool Authenticated,
    bool Connected,
    string? AccountId
);

public record AccountInfo(
    string AccountId,
    string? AccountVan,
    string? AccountTitle,
    string? DisplayName,
    string? AccountAlias,
    int? AccountStatus,
    string? Currency,
    string? Type,
    string? TradingType,
    bool? Faclient,
    string? ClearingStatus
);

public record Position(
    int? Conid,
    string? Ticker,
    double? PositionSize,
    double? MktPrice,
    double? MktValue,
    double? AvgCost,
    double? UnrealizedPnl,
    double? RealizedPnl,
    string? Sector,
    string? SecType,
    string? Currency
);

public record MarketDataSnapshot(
    int Conid,
    Dictionary<string, object> Data
);

public record OrderRequest(
    int Conid,
    string OrderType,
    string Side,
    double Quantity,
    double? Price = null,
    string Tif = "DAY",
    double? AuxPrice = null
);

public record MarketDataRequest(
    List<int> Conids,
    List<string> Fields
);

public record ContractSearchRequest(
    string Symbol,
    bool Name = false,
    string SecType = "STK"
);

public class IBKRApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public IBKRApiClient(string baseUrl = "http://127.0.0.1:8000", HttpClient? httpClient = null)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = httpClient ?? new HttpClient();
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    // Health and Status Methods
    public async Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/health", cancellationToken);
        await EnsureSuccessAsync(response);
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<HealthResponse>(json, _jsonOptions)!;
    }

    public async Task<Dictionary<string, object>> TickleAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/tickle", cancellationToken);
        await EnsureSuccessAsync(response);
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions)!;
    }

    // Account Methods
    public async Task<List<AccountInfo>> GetPortfolioAccountsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/portfolio/accounts", cancellationToken);
        await EnsureSuccessAsync(response);
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<AccountInfo>>(json, _jsonOptions)!;
    }

    public async Task<Dictionary<string, object>> GetAccountSummaryAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/portfolio/{accountId}/summary", cancellationToken);
        await EnsureSuccessAsync(response);
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions)!;
    }

    public async Task<Dictionary<string, object>> GetLedgerAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/portfolio/{accountId}/ledger", cancellationToken);
        await EnsureSuccessAsync(response);
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions)!;
    }

    // Portfolio Methods
    public async Task<List<Position>> GetPositionsAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/portfolio/{accountId}/positions", cancellationToken);
        await EnsureSuccessAsync(response);
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<Position>>(json, _jsonOptions)!;
    }

    public async Task<List<Position>> GetPositionsPagedAsync(string accountId, int page = 0, 
        string? model = null, string? sort = null, string? direction = null, string? period = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (model != null) queryParams.Add($"model={model}");
        if (sort != null) queryParams.Add($"sort={sort}");
        if (direction != null) queryParams.Add($"direction={direction}");
        if (period != null) queryParams.Add($"period={period}");
        
        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        
        var response = await _httpClient.GetAsync($"{_baseUrl}/portfolio/{accountId}/positions/{page}{queryString}", cancellationToken);
        await EnsureSuccessAsync(response);
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<Position>>(json, _jsonOptions)!;
    }

    // Market Data Methods
    public async Task<List<Dictionary<string, object>>> GetMarketDataSnapshotAsync(MarketDataRequest request, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync($"{_baseUrl}/iserver/marketdata/snapshot", content, cancellationToken);
        await EnsureSuccessAsync(response);
        
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(responseJson, _jsonOptions)!;
    }

    public async Task<Dictionary<string, object>> GetMarketDataHistoryAsync(string conid, string bar, 
        string? exchange = null, string? period = "1w", bool? outsideRth = null, string? startTime = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string> { $"conid={conid}", $"bar={bar}" };
        if (exchange != null) queryParams.Add($"exchange={exchange}");
        if (period != null) queryParams.Add($"period={period}");
        if (outsideRth.HasValue) queryParams.Add($"outside_rth={outsideRth.Value.ToString().ToLower()}");
        if (startTime != null) queryParams.Add($"start_time={startTime}");
        
        var queryString = "?" + string.Join("&", queryParams);
        
        var response = await _httpClient.GetAsync($"{_baseUrl}/iserver/marketdata/history{queryString}", cancellationToken);
        await EnsureSuccessAsync(response);
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions)!;
    }

    // Contract Search Methods
    public async Task<List<Dictionary<string, object>>> SearchContractsAsync(ContractSearchRequest request, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync($"{_baseUrl}/iserver/secdef/search", content, cancellationToken);
        await EnsureSuccessAsync(response);
        
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(responseJson, _jsonOptions)!;
    }

    public async Task<List<Dictionary<string, object>>> GetSecurityDefinitionsAsync(string conids, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/trsrv/secdef?conids={conids}", cancellationToken);
        await EnsureSuccessAsync(response);
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json, _jsonOptions)!;
    }

    // Order Methods
    public async Task<List<Dictionary<string, object>>> GetLiveOrdersAsync(string? filters = null, bool? force = null, string? accountId = null, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (filters != null) queryParams.Add($"filters={filters}");
        if (force.HasValue) queryParams.Add($"force={force.Value.ToString().ToLower()}");
        if (accountId != null) queryParams.Add($"account_id={accountId}");
        
        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        
        var response = await _httpClient.GetAsync($"{_baseUrl}/iserver/account/orders{queryString}", cancellationToken);
        await EnsureSuccessAsync(response);
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json, _jsonOptions)!;
    }

    public async Task<Dictionary<string, object>> PlaceOrderAsync(string accountId, OrderRequest order, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(order, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync($"{_baseUrl}/iserver/account/{accountId}/orders", content, cancellationToken);
        await EnsureSuccessAsync(response);
        
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson, _jsonOptions)!;
    }

    public async Task<List<Dictionary<string, object>>> GetTradesAsync(string? days = null, string? accountId = null, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (days != null) queryParams.Add($"days={days}");
        if (accountId != null) queryParams.Add($"account_id={accountId}");
        
        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        
        var response = await _httpClient.GetAsync($"{_baseUrl}/iserver/account/trades{queryString}", cancellationToken);
        await EnsureSuccessAsync(response);
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json, _jsonOptions)!;
    }

    // Helper Methods
    private async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"HTTP {(int)response.StatusCode} {response.StatusCode}: {error}");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Example usage program
public class Program
{
    public static async Task Main(string[] args)
    {
        using var client = new IBKRApiClient("http://127.0.0.1:8000");
        
        try
        {
            // Check service health
            Console.WriteLine("Checking service health...");
            var health = await client.GetHealthAsync();
            Console.WriteLine($"Service Status: {health.Status}");
            Console.WriteLine($"Authenticated: {health.Authenticated}");
            Console.WriteLine($"Account ID: {health.AccountId}");
            
            if (!health.Authenticated)
            {
                Console.WriteLine("Service is not authenticated. Please check IBKR connection.");
                return;
            }
            
            // Get accounts
            Console.WriteLine("\nGetting accounts...");
            var accounts = await client.GetPortfolioAccountsAsync();
            foreach (var account in accounts)
            {
                Console.WriteLine($"Account: {account.AccountId} - {account.DisplayName}");
            }
            
            if (accounts.Count == 0)
            {
                Console.WriteLine("No accounts found.");
                return;
            }
            
            var accountId = accounts[0].AccountId;
            
            // Get account summary
            Console.WriteLine($"\nGetting account summary for {accountId}...");
            var summary = await client.GetAccountSummaryAsync(accountId);
            Console.WriteLine($"Account Summary: {JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true })}");
            
            // Get positions
            Console.WriteLine($"\nGetting positions for {accountId}...");
            var positions = await client.GetPositionsAsync(accountId);
            foreach (var position in positions.Take(5)) // Show first 5 positions
            {
                Console.WriteLine($"Position: {position.Ticker} - {position.PositionSize} shares @ ${position.MktPrice} = ${position.MktValue}");
            }
            
            // Search for a contract (Apple stock)
            Console.WriteLine("\nSearching for AAPL contract...");
            var searchRequest = new ContractSearchRequest("AAPL", false, "STK");
            var contracts = await client.SearchContractsAsync(searchRequest);
            if (contracts.Count > 0)
            {
                Console.WriteLine($"Found {contracts.Count} contracts for AAPL");
                Console.WriteLine($"First contract: {JsonSerializer.Serialize(contracts[0], new JsonSerializerOptions { WriteIndented = true })}");
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
