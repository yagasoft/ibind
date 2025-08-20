using System;
using System.Collections.Generic;
using System.IO;

namespace Ibkr.OAuth;

public class OAuth1aConfig
{
    public string OAuthRestUrl { get; set; } = "https://api.ibkr.com/v1/api/";
    public string LiveSessionTokenEndpoint { get; set; } = "oauth/live_session_token";
    public string AccessToken { get; set; } = string.Empty;
    public string AccessTokenSecret { get; set; } = string.Empty;
    public string ConsumerKey { get; set; } = string.Empty;
    public string DhPrime { get; set; } = string.Empty;
    public string EncryptionKeyPath { get; set; } = string.Empty;
    public string SignatureKeyPath { get; set; } = string.Empty;
    public int DhGenerator { get; set; } = 2;
    public string Realm { get; set; } = "limited_poa";

    public void Verify()
    {
        var missing = new List<string>();
        if (string.IsNullOrEmpty(OAuthRestUrl)) missing.Add(nameof(OAuthRestUrl));
        if (string.IsNullOrEmpty(LiveSessionTokenEndpoint)) missing.Add(nameof(LiveSessionTokenEndpoint));
        if (string.IsNullOrEmpty(AccessToken)) missing.Add(nameof(AccessToken));
        if (string.IsNullOrEmpty(AccessTokenSecret)) missing.Add(nameof(AccessTokenSecret));
        if (string.IsNullOrEmpty(ConsumerKey)) missing.Add(nameof(ConsumerKey));
        if (string.IsNullOrEmpty(DhPrime)) missing.Add(nameof(DhPrime));
        if (string.IsNullOrEmpty(EncryptionKeyPath)) missing.Add(nameof(EncryptionKeyPath));
        if (string.IsNullOrEmpty(SignatureKeyPath)) missing.Add(nameof(SignatureKeyPath));
        if (missing.Count > 0)
            throw new ArgumentException($"OAuth1aConfig is missing required parameters: {string.Join(", ", missing)}");
        var missingFiles = new List<string>();
        if (!File.Exists(EncryptionKeyPath)) missingFiles.Add(nameof(EncryptionKeyPath));
        if (!File.Exists(SignatureKeyPath)) missingFiles.Add(nameof(SignatureKeyPath));
        if (missingFiles.Count > 0)
            throw new ArgumentException($"OAuth1aConfig file paths do not exist: {string.Join(", ", missingFiles)}");
    }
}
