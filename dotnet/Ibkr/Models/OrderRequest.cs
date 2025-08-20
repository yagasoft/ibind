using System.Collections.Generic;

namespace Ibkr.Models;

public class OrderRequest
{
    public int? Conid { get; set; }
    public string Side { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public string OrderType { get; set; } = string.Empty;
    public string AcctId { get; set; } = string.Empty;
    public double? Price { get; set; }
    public string? Conidex { get; set; }
    public string? SecType { get; set; }
    public string? Coid { get; set; }
    public string? ParentId { get; set; }
    public string? ListingExchange { get; set; }
    public bool? IsSingleGroup { get; set; }
    public bool? OutsideRth { get; set; }
    public double? AuxPrice { get; set; }
    public string? Ticker { get; set; }
    public string? Tif { get; set; } = "GTC";
    public double? TrailingAmt { get; set; }
    public string? TrailingType { get; set; }
    public string? Referrer { get; set; }
    public double? CashQty { get; set; }
    public double? FxQty { get; set; }
    public bool? UseAdaptive { get; set; }
    public bool? IsCcyConv { get; set; }
    public string? AllocationMethod { get; set; }
    public string? Strategy { get; set; }
    public Dictionary<string, object>? StrategyParameters { get; set; }
    public bool? IsClose { get; set; }
}
