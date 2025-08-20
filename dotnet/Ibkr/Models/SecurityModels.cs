using System.Collections.Generic;

namespace Ibkr.Models;

public class SecurityStocksBySymbolRequest
{
    public IEnumerable<StockQuery> Queries { get; set; } = new List<StockQuery>();
    public bool? DefaultFiltering { get; set; }
}

public class ContractDetails
{
    public long Conid { get; set; }
    public string Exchange { get; set; } = string.Empty;
    public string SecType { get; set; } = string.Empty;
    public string? Currency { get; set; }
    public bool? IsUS { get; set; }
}

public class SecurityDefinition
{
    public string Symbol { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Currency { get; set; }
    public List<ContractDetails>? Contracts { get; set; }
}

public class SecurityStocksBySymbolResponse
{
    public Dictionary<string, List<SecurityDefinition>> Stocks { get; set; } = new();
}

