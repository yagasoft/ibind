using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ibkr.Models;

public class PortfolioAccount
{
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("PrepaidCrypto-Z")]
    public bool PrepaidCryptoZ { get; set; }

    [JsonPropertyName("PrepaidCrypto-P")]
    public bool PrepaidCryptoP { get; set; }

    public bool BrokerageAccess { get; set; }

    public string AccountId { get; set; } = string.Empty;
    public string AccountVan { get; set; } = string.Empty;
    public string AccountTitle { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AccountAlias { get; set; }
    public long AccountStatus { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string TradingType { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string IbEntity { get; set; } = string.Empty;
    public bool Faclient { get; set; }
    public string ClearingStatus { get; set; } = string.Empty;
    public bool Covestor { get; set; }
    public bool NoClientTrading { get; set; }
    public bool TrackVirtualFXPortfolio { get; set; }
    public string AcctCustType { get; set; } = string.Empty;
    public ParentAccountInfo Parent { get; set; } = new();
    public string Desc { get; set; } = string.Empty;
}

public class ParentAccountInfo
{
    public List<string> Mmc { get; set; } = new();
    public string AccountId { get; set; } = string.Empty;
    public bool IsMParent { get; set; }
    public bool IsMChild { get; set; }
    public bool IsMultiplex { get; set; }
}

public class AccountAlias
{
    public string AccountId { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
}

public class BrokerageAccountsResponse
{
    public List<string> Accounts { get; set; } = new();
    public List<AccountAlias>? AccountsWithAlias { get; set; }
    public string? SelectedAccount { get; set; }
}

