using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };

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

    public async Task<Result<AuthenticationStatusResponse>> AuthenticationStatusAsync()
    {
        var res = await PostAsync("iserver/auth/status");
        var data = JsonSerializer.Deserialize<AuthenticationStatusResponse>(res.Data.RootElement.GetRawText(), JsonOptions);
        return new Result<AuthenticationStatusResponse>(data, res.Request);
    }

    // Accounts
    public async Task<Result<List<PortfolioAccount>>> PortfolioAccountsAsync()
    {
        var res = await GetAsync("portfolio/accounts");
        var data = JsonSerializer.Deserialize<List<PortfolioAccount>>(res.Data.RootElement.GetRawText(), JsonOptions);
        return new Result<List<PortfolioAccount>>(data, res.Request);
    }

    public async Task<Result<BrokerageAccountsResponse>> ReceiveBrokerageAccountsAsync()
    {
        var res = await GetAsync("iserver/accounts");
        var data = JsonSerializer.Deserialize<BrokerageAccountsResponse>(res.Data.RootElement.GetRawText(), JsonOptions);
        return new Result<BrokerageAccountsResponse>(data, res.Request);
    }

    // Portfolio
    public async Task<Result<PortfolioSummary>> PortfolioSummaryAsync(string? accountId = null)
    {
        accountId ??= AccountId;
        var res = await GetAsync($"portfolio/{accountId}/summary");
        var data = JsonSerializer.Deserialize<PortfolioSummary>(res.Data.RootElement.GetRawText(), JsonOptions);
        return new Result<PortfolioSummary>(data, res.Request);
    }

    public async Task<Result<PositionInfo>> PositionAndContractInfoAsync(string conid)
    {
        var res = await GetAsync($"portfolio/positions/{conid}");
        var data = JsonSerializer.Deserialize<PositionInfo>(res.Data.RootElement.GetRawText(), JsonOptions);
        return new Result<PositionInfo>(data, res.Request);
    }

    public async Task<Result<List<PositionInfo>>> PositionsByConidAsync(string? accountId, string conid)
    {
        accountId ??= AccountId;
        var res = await GetAsync($"portfolio/{accountId}/position/{conid}");
        var data = JsonSerializer.Deserialize<List<PositionInfo>>(res.Data.RootElement.GetRawText(), JsonOptions);
        return new Result<List<PositionInfo>>(data, res.Request);
    }

    // Contract
    public async Task<Result<SecurityStocksBySymbolResponse>> SecurityStocksBySymbolAsync(SecurityStocksBySymbolRequest request)
    {
        var symbols = StockUtils.QueryToSymbols(request.Queries);
        var result = await GetAsync("trsrv/stocks", new Dictionary<string,string>{{"symbols", symbols}});
        var filtered = StockUtils.FilterStocks(request.Queries, result.Data, request.DefaultFiltering ?? true);
        var json = JsonSerializer.Serialize(filtered);
        var stocks = JsonSerializer.Deserialize<Dictionary<string, List<SecurityDefinition>>>(json, JsonOptions)!;
        var resp = new SecurityStocksBySymbolResponse { Stocks = stocks };
        return new Result<SecurityStocksBySymbolResponse>(resp, result.Request);
    }

    public async Task<Result<SearchContractRulesResponse>> SearchContractRulesAsync(SearchContractRulesRequest request)
    {
        var body = new Dictionary<string, object?> { ["conid"] = request.Conid };
        if (request.Exchange != null) body["exchange"] = request.Exchange;
        if (request.IsBuy != null) body["isBuy"] = request.IsBuy;
        if (request.ModifyOrder != null) body["modifyOrder"] = request.ModifyOrder;
        if (request.OrderId != null) body["orderId"] = request.OrderId;
        var res = await PostAsync("iserver/contract/rules", body);
        var rules = JsonSerializer.Deserialize<List<ContractRule>>(res.Data.RootElement.GetRawText(), JsonOptions) ?? new();
        var resp = new SearchContractRulesResponse { Rules = rules };
        return new Result<SearchContractRulesResponse>(resp, res.Request);
    }

    // Market data
    public async Task<Result<LiveMarketdataSnapshotResponse>> LiveMarketdataSnapshotAsync(LiveMarketdataSnapshotRequest request)
    {
        var query = new Dictionary<string,string>{{"conids", string.Join(",", request.Conids)}, {"fields", string.Join(",", request.Fields)}};
        var res = await GetAsync("iserver/marketdata/snapshot", query);
        var snapshots = new List<MarketDataSnapshot>();
        foreach (var el in res.Data.RootElement.EnumerateArray())
        {
            var snap = new MarketDataSnapshot { Conid = el.GetProperty("conid").GetInt64() };
            var data = new Dictionary<string,string>();
            foreach (var prop in el.EnumerateObject())
            {
                if (prop.NameEquals("conid")) continue;
                data[prop.Name] = prop.Value.ToString();
            }
            snap.Data = data;
            snapshots.Add(snap);
        }
        var resp = new LiveMarketdataSnapshotResponse { Snapshots = snapshots };
        return new Result<LiveMarketdataSnapshotResponse>(resp, res.Request);
    }

    // Orders
    public async Task<Result<LiveOrdersResponse>> LiveOrdersAsync(LiveOrdersRequest? request = null)
    {
        Dictionary<string,string>? query = null;
        if (request != null)
        {
            query = new Dictionary<string,string>();
            if (request.Filters != null) query["filters"] = string.Join(",", request.Filters);
            if (request.Force != null) query["force"] = request.Force.Value ? "true" : "false";
            if (request.AccountId != null) query["accountId"] = request.AccountId;
        }
        var res = await GetAsync("iserver/account/orders", query);
        var orders = JsonSerializer.Deserialize<List<LiveOrder>>(res.Data.RootElement.GetRawText(), JsonOptions) ?? new();
        var resp = new LiveOrdersResponse { Orders = orders };
        return new Result<LiveOrdersResponse>(resp, res.Request);
    }

    public async Task<Result<List<PlaceOrderResponse>>> PlaceOrderAsync(PlaceOrderRequest request, string? accountId = null)
    {
        accountId ??= AccountId;
        var parsed = request.Orders.Select(o => OrderUtils.ParseOrderRequest(o)).ToList();
        var body = new Dictionary<string, object?> { ["orders"] = parsed };
        if (request.Answers != null && request.Answers.Count > 0) body["answers"] = request.Answers;
        var res = await PostAsync($"iserver/account/{accountId}/orders", body);
        var data = JsonSerializer.Deserialize<List<PlaceOrderResponse>>(res.Data.RootElement.GetRawText(), JsonOptions);
        return new Result<List<PlaceOrderResponse>>(data, res.Request);
    }
}
