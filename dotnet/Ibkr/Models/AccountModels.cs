using System.Collections.Generic;

namespace Ibkr.Models;

public class PortfolioAccount
{
    public string AccountId { get; set; } = string.Empty;
    public string? AccountType { get; set; }
    public string? DisplayName { get; set; }
    public string? Currency { get; set; }
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

