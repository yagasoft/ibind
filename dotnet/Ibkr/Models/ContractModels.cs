using System.Collections.Generic;

namespace Ibkr.Models;

public class SearchContractRulesRequest
{
    public string Conid { get; set; } = string.Empty;
    public string? Exchange { get; set; }
    public bool? IsBuy { get; set; }
    public bool? ModifyOrder { get; set; }
    public int? OrderId { get; set; }
}

public class ContractRule
{
    public string? Key { get; set; }
    public string? Value { get; set; }
}

public class SearchContractRulesResponse
{
    public List<ContractRule> Rules { get; set; } = new();
}

