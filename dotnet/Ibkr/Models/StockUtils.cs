using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Ibkr.Models;

public static class StockUtils
{
    private static bool Matches(Dictionary<string, object?> data, Dictionary<string, object>? conditions)
    {
        if (conditions == null) return true;
        foreach (var kv in conditions)
        {
            if (!data.TryGetValue(kv.Key, out var v) || v == null || !v.Equals(kv.Value))
                return false;
        }
        return true;
    }

    private static List<Dictionary<string, object?>> ProcessInstruments(List<Dictionary<string, object?>> instruments, string? nameMatch, Dictionary<string, object>? instrumentConditions, Dictionary<string, object>? contractConditions)
    {
        var filtered = new List<Dictionary<string, object?>>();
        foreach (var instrument in instruments)
        {
            if (nameMatch != null && instrument.TryGetValue("name", out var n) && n is string ns && !ns.Contains(nameMatch, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!Matches(instrument.ToDictionary(k => k.Key, k => k.Value), instrumentConditions))
                continue;
            if (contractConditions != null && instrument.TryGetValue("contracts", out var contractsObj) && contractsObj is JsonElement contractsEl && contractsEl.ValueKind == JsonValueKind.Array)
            {
                var filteredContracts = contractsEl.EnumerateArray().Where(c => Matches(JsonSerializer.Deserialize<Dictionary<string, object?>>(c.GetRawText())!, contractConditions)).Select(c => JsonSerializer.Deserialize<Dictionary<string, object?>>(c.GetRawText())!).ToList();
                if (filteredContracts.Count == 0) continue;
                instrument["contracts"] = filteredContracts;
            }
            filtered.Add(instrument);
        }
        return filtered;
    }

    public static Dictionary<string, object> FilterStocks(IEnumerable<object> queries, JsonDocument result, bool defaultFiltering = true)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, object?>>>>(result.RootElement.GetRawText())!;
        var stocks = new Dictionary<string, object>();
        foreach (var q in queries)
        {
            var query = q as StockQuery ?? new StockQuery { Symbol = q.ToString()! };
            var symbol = query.Symbol;
            if (!data.TryGetValue(symbol, out var instruments) || instruments.Count == 0)
                continue;
            // default filtering: ensure instrument country 'US'
            if (defaultFiltering)
            {
                instruments = instruments.Select(i =>
                {
                    if (i.TryGetValue("contracts", out var cobj) && cobj is JsonElement cel && cel.ValueKind == JsonValueKind.Array)
                    {
                        var contracts = cel.EnumerateArray().Where(c => {
                            var cd = JsonSerializer.Deserialize<Dictionary<string, object?>>(c.GetRawText())!;
                            return !cd.TryGetValue("isUS", out var v) || v?.ToString() == "True" || v?.ToString() == "true";
                        }).Select(c => JsonSerializer.Deserialize<Dictionary<string, object?>>(c.GetRawText())!).ToList();
                        i["contracts"] = contracts;
                    }
                    return i;
                }).ToList();
            }
            var filteredInstruments = ProcessInstruments(instruments, query.NameMatch, query.InstrumentConditions, query.ContractConditions);
            stocks[symbol] = filteredInstruments;
        }
        return stocks;
    }

    public static string QueryToSymbols(IEnumerable<object> queries)
    {
        return string.Join(",", queries.Select(q => q is StockQuery sq ? sq.Symbol : q.ToString()));
    }
}
