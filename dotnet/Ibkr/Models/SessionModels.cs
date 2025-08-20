using System;
using System.Text.Json.Serialization;

namespace Ibkr.Models;

public class AuthenticationStatusResponse
{
    public bool Authenticated { get; set; }
    public bool Competing { get; set; }
    public bool Connected { get; set; }

    [JsonPropertyName("MAC")]
    public string? Mac { get; set; }

    public string? Fail { get; set; }

    [JsonPropertyName("hardware_info")]
    public string? HardwareInfo { get; set; }

    public string? Message { get; set; }

    public AuthServerInfo? ServerInfo { get; set; }
}

public class AuthServerInfo
{
    public string? ServerName { get; set; }
    public string? ServerVersion { get; set; }
}

