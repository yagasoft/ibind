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

public sealed class PositionInfo
{
    // Required / always present in samples
    [JsonPropertyName("acctId")] public string? AccountId { get; set; }
    [JsonPropertyName("conid")] public long? Conid { get; set; }
    [JsonPropertyName("contractDesc")] public string? ContractDesc { get; set; }
    [JsonPropertyName("position")] public double? Position { get; set; }
    [JsonPropertyName("mktPrice")] public double? MarketPrice { get; set; }
    [JsonPropertyName("mktValue")] public double? MarketValue { get; set; }
    [JsonPropertyName("currency")] public string? Currency { get; set; }

    // Average cost/price and PnL
    [JsonPropertyName("avgCost")] public double? AverageCost { get; set; }
    [JsonPropertyName("avgPrice")] public double? AveragePrice { get; set; }
    [JsonPropertyName("realizedPnl")] public double? RealizedPnl { get; set; }
    [JsonPropertyName("unrealizedPnl")] public double? UnrealizedPnl { get; set; }

    // Contract metadata
    [JsonPropertyName("assetClass")] public string? AssetClass { get; set; }   // e.g., STK/ETF
    [JsonPropertyName("type")] public string? Type { get; set; }               // e.g., ETF (from sample)
    [JsonPropertyName("ticker")] public string? Ticker { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("fullName")] public string? FullName { get; set; }
    [JsonPropertyName("chineseName")] public string? ChineseName { get; set; }
    [JsonPropertyName("listingExchange")] public string? ListingExchange { get; set; }
    [JsonPropertyName("countryCode")] public string? CountryCode { get; set; }
    [JsonPropertyName("hasOptions")] public bool? HasOptions { get; set; }
    [JsonPropertyName("isUS")] public bool? IsUS { get; set; }
    [JsonPropertyName("isEventContract")] public bool? IsEventContract { get; set; }

    // Dates / option-like fields (often null in sample)
    [JsonPropertyName("expiry")] public string? Expiry { get; set; }
    [JsonPropertyName("putOrCall")] public string? PutOrCall { get; set; }
    [JsonPropertyName("multiplier")] public double? Multiplier { get; set; }

    // NOTE: strike is "0" (string) in the sample although docs say number.
    // Keep string? to match the wire payload robustly.
    [JsonPropertyName("strike")] public string? Strike { get; set; }

    [JsonPropertyName("exerciseStyle")] public string? ExerciseStyle { get; set; }

    // Underlying / model
    [JsonPropertyName("undConid")] public long? UnderlyingConid { get; set; }
    [JsonPropertyName("model")] public string? Model { get; set; }

    // Exchanges
    [JsonPropertyName("exchs")] public string? Exchanges { get; set; }
    [JsonPropertyName("allExchanges")] public string? AllExchanges { get; set; }
    [JsonPropertyName("conExchMap")] public List<string>? ConExchMap { get; set; }

    // Grouping / sector info
    [JsonPropertyName("group")] public string? Group { get; set; }
    [JsonPropertyName("sector")] public string? Sector { get; set; }
    [JsonPropertyName("sectorGroup")] public string? SectorGroup { get; set; }

    // Display / increment rules (appear in sample)
    [JsonPropertyName("incrementRules")] public List<IncrementRule>? IncrementRules { get; set; }
    [JsonPropertyName("displayRule")] public DisplayRule? DisplayRule { get; set; }

    // Misc
    [JsonPropertyName("pageSize")] public int? PageSize { get; set; }

    // Appears in sample as an integer 28; API doc doesn’t list it.
    // Keep as nullable int to match the sample without overcommitting semantics.
    [JsonPropertyName("time")] public int? Time { get; set; }

    // “Base” fields are present in docs but not in the sample. Keep them optional.
    [JsonPropertyName("baseMktValue")] public double? BaseMarketValue { get; set; }
    [JsonPropertyName("baseMktPrice")] public double? BaseMarketPrice { get; set; }
    [JsonPropertyName("baseAvgCost")] public double? BaseAverageCost { get; set; }
    [JsonPropertyName("baseAvgPrice")] public double? BaseAveragePrice { get; set; }
    [JsonPropertyName("baseRealizedPnl")] public double? BaseRealizedPnl { get; set; }
    [JsonPropertyName("baseUnrealizedPnl")] public double? BaseUnrealizedPnl { get; set; }

    // Fields mentioned in docs but absent in the sample. Keep them for completeness.
    [JsonPropertyName("lastTradingDay")] public string? LastTradingDay { get; set; }
    [JsonPropertyName("undComp")] public string? UnderlyingComposite { get; set; }
    [JsonPropertyName("undSym")] public string? UnderlyingSymbol { get; set; }
}

// ---- Nested shapes observed in the sample ----

public sealed class DisplayRule
{
    [JsonPropertyName("magnification")] public int? Magnification { get; set; }
    [JsonPropertyName("displayRuleStep")] public List<DisplayRuleStep>? Steps { get; set; }
}

public sealed class DisplayRuleStep
{
    [JsonPropertyName("decimalDigits")] public int? DecimalDigits { get; set; }
    [JsonPropertyName("lowerEdge")] public double? LowerEdge { get; set; }
    [JsonPropertyName("wholeDigits")] public int? WholeDigits { get; set; }
}
