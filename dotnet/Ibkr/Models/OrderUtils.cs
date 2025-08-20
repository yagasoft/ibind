using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Ibkr.Models;

public static class OrderUtils
{
    private static readonly Dictionary<string,string> Mapping = new()
    {
        ["Conid"] = "conid",
        ["Side"] = "side",
        ["Quantity"] = "quantity",
        ["OrderType"] = "orderType",
        ["Price"] = "price",
        ["Coid"] = "cOID",
        ["AcctId"] = "acctId",
        ["Conidex"] = "conidex",
        ["SecType"] = "secType",
        ["ParentId"] = "parentId",
        ["ListingExchange"] = "listingExchange",
        ["IsSingleGroup"] = "isSingleGroup",
        ["OutsideRth"] = "outsideRTH",
        ["AuxPrice"] = "auxPrice",
        ["Ticker"] = "ticker",
        ["Tif"] = "tif",
        ["TrailingAmt"] = "trailingAmt",
        ["TrailingType"] = "trailingType",
        ["Referrer"] = "referrer",
        ["CashQty"] = "cashQty",
        ["FxQty"] = "fxQty",
        ["UseAdaptive"] = "useAdaptive",
        ["IsCcyConv"] = "isCcyConv",
        ["AllocationMethod"] = "allocationMethod",
        ["Strategy"] = "strategy",
        ["StrategyParameters"] = "strategyParameters",
        ["IsClose"] = "isClose"
    };

    public static Dictionary<string, object?> ParseOrderRequest(OrderRequest order)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var kv in Mapping)
        {
            var prop = order.GetType().GetProperty(kv.Key);
            var value = prop?.GetValue(order);
            if (value != null)
                dict[kv.Value] = value;
        }
        if (dict.ContainsKey("conidex") && dict.ContainsKey("conid"))
            throw new System.ArgumentException("Both 'conidex' and 'conid' are provided. When using 'conidex', specify conid=null.");
        return dict;
    }
}
