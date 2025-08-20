using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ibkr.Models;

public class SearchContractRulesRequest
{
    public string Conid { get; set; } = string.Empty;
    public string? Exchange { get; set; }
    public bool? IsBuy { get; set; }
    public bool? ModifyOrder { get; set; }
    public int? OrderId { get; set; }
}

public sealed class ContractRule
{
    [JsonPropertyName("algoEligible")]
    public bool? AlgoEligible { get; set; }

    [JsonPropertyName("allOrNoneEligible")]
    public bool? AllOrNoneEligible { get; set; }

    [JsonPropertyName("costReport")]
    public bool? CostReport { get; set; }

    // Sample shows account ids as strings (e.g., "U20721060")
    [JsonPropertyName("canTradeAcctIds")]
    public List<string>? CanTradeAcctIds { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("orderTypes")]
    public List<string>? OrderTypes { get; set; }

    // Note: docs spell "ibalgoTypes"; sample uses "ibAlgoTypes"
    // Prefer the sample field name (JSON is case-sensitive on property names).
    [JsonPropertyName("ibAlgoTypes")]
    public List<string>? IbAlgoTypes { get; set; }

    [JsonPropertyName("fraqTypes")]
    public List<string>? FraqTypes { get; set; }

    [JsonPropertyName("cqtTypes")]
    public List<string>? CqtTypes { get; set; }

    // Key is order type (e.g., "LMT"), value is a typed preset holder
    [JsonPropertyName("orderDefaults")]
    public Dictionary<string, OrderDefaultsEntry>? OrderDefaults { get; set; }

    [JsonPropertyName("orderTypesOutside")]
    public List<string>? OrderTypesOutside { get; set; }

    [JsonPropertyName("defaultSize")]
    public int? DefaultSize { get; set; }

    [JsonPropertyName("cashSize")]
    public double? CashSize { get; set; }

    [JsonPropertyName("sizeIncrement")]
    public int? SizeIncrement { get; set; }

    // The API returns complex “encoded” strings here; keep string list
    [JsonPropertyName("tifTypes")]
    public List<string>? TifTypes { get; set; }

    // Docs mention "defaultTIF"; sample shows an object "tifDefaults"
    [JsonPropertyName("defaultTIF")]
    public string? DefaultTif { get; set; }

    [JsonPropertyName("tifDefaults")]
    public TifDefaults? TifDefaults { get; set; }

    [JsonPropertyName("limitPrice")]
    public double? LimitPrice { get; set; }

    [JsonPropertyName("stopprice")]
    public double? StopPrice { get; set; }

    [JsonPropertyName("orderOrigination")]
    public int? OrderOrigination { get; set; }

    [JsonPropertyName("preview")]
    public bool? Preview { get; set; }

    [JsonPropertyName("displaySize")]
    public double? DisplaySize { get; set; }

    [JsonPropertyName("fraqInt")]
    public int? FraqInt { get; set; }

    [JsonPropertyName("cashCcy")]
    public string? CashCcy { get; set; }

    [JsonPropertyName("cashQtyIncr")]
    public double? CashQtyIncr { get; set; }

    [JsonPropertyName("priceMagnifier")]
    public int? PriceMagnifier { get; set; }

    [JsonPropertyName("negativeCapable")]
    public bool? NegativeCapable { get; set; }

    [JsonPropertyName("incrementType")]
    public int? IncrementType { get; set; }

    [JsonPropertyName("incrementRules")]
    public List<IncrementRule>? IncrementRules { get; set; }

    [JsonPropertyName("hasSecondary")]
    public bool? HasSecondary { get; set; }

    [JsonPropertyName("increment")]
    public double? Increment { get; set; }

    [JsonPropertyName("incrementDigits")]
    public int? IncrementDigits { get; set; }
}

public sealed class OrderDefaultsEntry
{
    // All optional; many values come as strings in the API
    [JsonPropertyName("ORTH")]
    public bool? OutsideRegularTradingHours { get; set; }

    [JsonPropertyName("SP")]
    public string? StopPrice { get; set; }

    [JsonPropertyName("LP")]
    public string? LimitPrice { get; set; }

    [JsonPropertyName("PC")]
    public string? PriceCap { get; set; }

    [JsonPropertyName("TA")]
    public string? TrailingAmount { get; set; }

    [JsonPropertyName("TU")]
    public string? TrailingUnit { get; set; }

    [JsonPropertyName("ROA")]
    public string? RelativeOffsetAmount { get; set; }

    [JsonPropertyName("ROP")]
    public string? RelativeOffsetPercent { get; set; }

    [JsonPropertyName("TT")]
    public string? TouchTriggerPrice { get; set; }

    [JsonPropertyName("UNP")]
    public bool? UseNetPriceForBonds { get; set; }
}

public sealed class TifDefaults
{
    // Sample shows: {"TIF":"DAY","SIZE":"100.00","PMALGO":true}
    [JsonPropertyName("TIF")]
    public string? Tif { get; set; }

    [JsonPropertyName("SIZE")]
    public string? Size { get; set; }

    [JsonPropertyName("PMALGO")]
    public bool? PmAlgo { get; set; }
}

public sealed class IncrementRule
{
    [JsonPropertyName("lowerEdge")]
    public double? LowerEdge { get; set; }

    [JsonPropertyName("increment")]
    public double? Increment { get; set; }
}

public class SearchContractRulesResponse
{
    public List<ContractRule> Rules { get; set; } = new();
}

