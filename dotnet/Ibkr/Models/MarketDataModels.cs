using System.Collections.Generic;

namespace Ibkr.Models;

public class LiveMarketdataSnapshotRequest
{
    public List<string> Conids { get; set; } = new();
    public List<string> Fields { get; set; } = new();
}

public class MarketDataSnapshot
{
    public long Conid { get; set; }
    public Dictionary<string, string> Data { get; set; } = new();
}

public class LiveMarketdataSnapshotResponse
{
    public List<MarketDataSnapshot> Snapshots { get; set; } = new();
}

