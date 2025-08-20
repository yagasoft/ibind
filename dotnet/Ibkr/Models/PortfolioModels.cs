using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ibkr.Models;

public class PortfolioSummary
{
    [JsonPropertyName("acctId")]
    public string AccountId { get; set; } = string.Empty;

    [JsonPropertyName("acctVan")]
    public string AccountVan { get; set; } = string.Empty;

    public string Currency { get; set; } = string.Empty;
    public double? ExchangeRate { get; set; }
    public long? Timestamp { get; set; }
    public List<PortfolioSummaryItem> Values { get; set; } = new();
}

public class PortfolioSummaryItem
{
    [JsonPropertyName("acctId")]
    public string AccountId { get; set; } = string.Empty;

    public string Tag { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Currency { get; set; }

    [JsonPropertyName("desc")]
    public string? Description { get; set; }

    public bool? Summary { get; set; }
}

public class ContractInfo
{
    public long Conid { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string? LocalSymbol { get; set; }
    public string? SecType { get; set; }
    public string? Currency { get; set; }
    public string? Exchange { get; set; }
    public string? PrimaryExchange { get; set; }
    public string? Description { get; set; }
    public string? Multiplier { get; set; }
    public string? Expiry { get; set; }
    public double? Strike { get; set; }
    public string? Right { get; set; }
    public string? TradingClass { get; set; }
}

public class PositionInfo
{
    public ContractInfo Contract { get; set; } = new();
    public string Account { get; set; } = string.Empty;
    public double Position { get; set; }
    public double MarketPrice { get; set; }
    public double MarketValue { get; set; }
    public double AverageCost { get; set; }
    public double UnrealizedPnl { get; set; }
    public double RealizedPnl { get; set; }
    public double? MarkPrice { get; set; }
    public long? Timestamp { get; set; }
}

