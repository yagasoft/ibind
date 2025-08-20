using System.Collections.Generic;

namespace Ibkr.Models;

public class PortfolioSummaryItem
{
    public string Account { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Currency { get; set; }
    public string? Description { get; set; }
}

public class ContractInfo
{
    public long Conid { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string? SecType { get; set; }
    public string? Currency { get; set; }
    public string? Exchange { get; set; }
    public string? Description { get; set; }
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
}

