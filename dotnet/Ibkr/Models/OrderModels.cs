using System.Collections.Generic;

namespace Ibkr.Models;

public class LiveOrdersRequest
{
    public List<string>? Filters { get; set; }
    public bool? Force { get; set; }
    public string? AccountId { get; set; }
}

public enum OrderSide
{
    Buy,
    Sell
}

public enum OrderStatus
{
    PendingSubmit,
    Submitted,
    Filled,
    Cancelled,
    PendingCancel,
    Unknown
}

public class LiveOrder
{
    public long OrderId { get; set; }
    public long Conid { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public double Quantity { get; set; }
    public double FilledQuantity { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Unknown;
    public double? Price { get; set; }
    public double? AvgFillPrice { get; set; }
    public string? Tif { get; set; }
    public string? OrderType { get; set; }
}

public class LiveOrdersResponse
{
    public List<LiveOrder> Orders { get; set; } = new();
}

public class PlaceOrderRequest
{
    public List<OrderRequest> Orders { get; set; } = new();
    public Dictionary<string,bool>? Answers { get; set; }
}

public class PlaceOrderResponse
{
    public long OrderId { get; set; }
    public string? ClientOrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? WarningText { get; set; }
}

