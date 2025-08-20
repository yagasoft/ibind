using System.Collections.Generic;

namespace Ibkr.Models;

public class StockQuery
{
    public string Symbol { get; set; } = string.Empty;
    public string? NameMatch { get; set; }
    public Dictionary<string, object>? InstrumentConditions { get; set; }
    public Dictionary<string, object>? ContractConditions { get; set; }
}
