using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Ibkr.OAuth;
using Ibkr.Models;

namespace Ibkr;

public class IbkrSettings
{
    public string RestUrl { get; set; } = "https://api.ibkr.com/v1/api/";
    public string AccountId { get; set; } = string.Empty;
    public bool UseOauth { get; set; } = false;
    public OAuth1aConfig OAuth { get; set; } = new();
}

public class IbkrClient : RestClient
{
    private readonly bool _useOauth;
    private readonly OAuth1aConfig _oauthConfig;
    private string? _liveSessionToken;
    private long _liveSessionTokenExpires;
    public string AccountId { get; private set; }

    public IbkrClient(IbkrSettings settings) : base(settings.UseOauth ? settings.OAuth.OAuthRestUrl : settings.RestUrl)
    {
        _useOauth = settings.UseOauth;
        AccountId = settings.AccountId;
        _oauthConfig = settings.OAuth;
        if (_useOauth)
        {
            _oauthConfig.Verify();
            var lst = OAuth1aHelper.RequestLiveSessionTokenAsync(this, _oauthConfig).GetAwaiter().GetResult();
            _liveSessionToken = lst.Token;
            _liveSessionTokenExpires = lst.Expires;
        }
    }

    public static IbkrClient FromAppSettings(string path)
    {
        var json = File.ReadAllText(path);
        var root = JsonDocument.Parse(json).RootElement.GetProperty("Ibkr");
        var settings = JsonSerializer.Deserialize<IbkrSettings>(root.GetRawText())!;
        return new IbkrClient(settings);
    }

    protected override Task<Dictionary<string, string>> GetHeadersAsync(string method, string url)
    {
        if (!_useOauth) return Task.FromResult(new Dictionary<string,string>());
        if (url == $"{BaseUrl}{_oauthConfig.LiveSessionTokenEndpoint}")
            return Task.FromResult(new Dictionary<string,string>());
        var headers = OAuth1aHelper.GenerateOAuthHeaders(_oauthConfig, method, url, _liveSessionToken);
        return Task.FromResult(headers);
    }

    // Session & health
    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var res = await PostAsync("tickle");
            var auth = res.Data.RootElement.GetProperty("iserver").GetProperty("authStatus");
            bool authenticated = auth.GetProperty("authenticated").GetBoolean();
            bool competing = auth.GetProperty("competing").GetBoolean();
            bool connected = auth.GetProperty("connected").GetBoolean();
            return authenticated && !competing && connected;
        }
        catch
        {
            return false;
        }
    }

    public Task<Result<JsonDocument>> AuthenticationStatusAsync()
        => PostAsync("iserver/auth/status");

    // Accounts
    public Task<Result<JsonDocument>> PortfolioAccountsAsync()
        => GetAsync("portfolio/accounts");

    public Task<Result<JsonDocument>> ReceiveBrokerageAccountsAsync()
        => GetAsync("iserver/accounts");

    // Portfolio
    public Task<Result<JsonDocument>> PortfolioSummaryAsync(string? accountId = null)
    {
        accountId ??= AccountId;
        return GetAsync($"portfolio/{accountId}/summary");
    }

    public Task<Result<JsonDocument>> PositionAndContractInfoAsync(string conid)
        => GetAsync($"portfolio/positions/{conid}");

    public Task<Result<JsonDocument>> PositionsByConidAsync(string? accountId, string conid)
    {
        accountId ??= AccountId;
        return GetAsync($"portfolio/{accountId}/position/{conid}");
    }

    // Contract
    public async Task<Result<JsonDocument>> SecurityStocksBySymbolAsync(IEnumerable<object> queries, bool? defaultFiltering = null)
    {
        var symbols = StockUtils.QueryToSymbols(queries);
        var result = await GetAsync("trsrv/stocks", new Dictionary<string,string>{{"symbols", symbols}});
        var filtered = StockUtils.FilterStocks(queries, result.Data, defaultFiltering ?? true);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(filtered));
        return new Result<JsonDocument>(doc, result.Request);
    }

    public Task<Result<JsonDocument>> SearchContractRulesAsync(string conid, string? exchange = null, bool? isBuy = null, bool? modifyOrder = null, int? orderId = null)
    {
        var body = new Dictionary<string, object?>{{"conid", conid}};
        if (exchange != null) body["exchange"] = exchange;
        if (isBuy != null) body["isBuy"] = isBuy;
        if (modifyOrder != null) body["modifyOrder"] = modifyOrder;
        if (orderId != null) body["orderId"] = orderId;
        return PostAsync("iserver/contract/rules", body);
    }

    // Market data
    public Task<Result<JsonDocument>> LiveMarketdataSnapshotAsync(IEnumerable<string> conids, IEnumerable<string> fields)
    {
        var query = new Dictionary<string,string>{{"conids", string.Join(",", conids)}, {"fields", string.Join(",", fields)}};
        return GetAsync("iserver/marketdata/snapshot", query);
    }

    // Orders
    public Task<Result<JsonDocument>> LiveOrdersAsync(IEnumerable<string>? filters = null, bool? force = null, string? accountId = null)
    {
        var query = new Dictionary<string,string>();
        if (filters != null) query["filters"] = string.Join(",", filters);
        if (force != null) query["force"] = force.Value ? "true" : "false";
        if (accountId != null) query["accountId"] = accountId;
        return GetAsync("iserver/account/orders", query.Count > 0 ? query : null);
    }

    public Task<Result<JsonDocument>> PlaceOrderAsync(IEnumerable<OrderRequest> orders, Dictionary<string,bool> answers, string? accountId = null)
    {
        accountId ??= AccountId;
        var parsed = orders.Select(o => OrderUtils.ParseOrderRequest(o)).ToList();
        var body = new Dictionary<string, object?> { ["orders"] = parsed };
        return PostAsync($"iserver/account/{accountId}/orders", body);
    }
}
