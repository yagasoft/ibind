using System;

namespace Ibkr.Models;

public class AuthenticationStatusResponse
{
    public bool Authenticated { get; set; }
    public bool Competing { get; set; }
    public bool Connected { get; set; }
    public string? Message { get; set; }
}

