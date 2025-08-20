using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Numerics;
using System.Linq;
using System.Threading.Tasks;

namespace Ibkr.OAuth;

public static class OAuth1aHelper
{
    private const string EncodingName = "utf-8";

    public static async Task<(string Token, long Expires, string Signature)> RequestLiveSessionTokenAsync(RestClient client, OAuth1aConfig config)
    {
        var (prepend, extraHeaders, dhRandom) = PrepareOauth(config);
        var headers = GenerateOAuthHeaders(config, "POST", $"{client.BaseUrl}{config.LiveSessionTokenEndpoint}", null, extraHeaders, null, "RSA-SHA256", prepend);
        var result = await client.PostAsync(config.LiveSessionTokenEndpoint, body: null, extraHeaders: headers);
        var data = result.Data.RootElement;
        var expires = data.GetProperty("live_session_token_expiration").GetInt64();
        var dhResponse = data.GetProperty("diffie_hellman_response").GetString()!;
        var signature = data.GetProperty("live_session_token_signature").GetString()!;
        var token = CalculateLiveSessionToken(config.DhPrime, dhRandom, dhResponse, prepend);
        return (token, expires, signature);
    }

    public static Dictionary<string, string> GenerateOAuthHeaders(
        OAuth1aConfig config,
        string requestMethod,
        string requestUrl,
        string? liveSessionToken = null,
        Dictionary<string,string>? extraHeaders = null,
        Dictionary<string,string>? requestParams = null,
        string signatureMethod = "HMAC-SHA256",
        string? prepend = null)
    {
        var headers = new Dictionary<string, string>
        {
            ["oauth_consumer_key"] = config.ConsumerKey,
            ["oauth_nonce"] = GenerateOauthNonce(),
            ["oauth_signature_method"] = signatureMethod,
            ["oauth_timestamp"] = GenerateRequestTimestamp(),
            ["oauth_token"] = config.AccessToken
        };
        if (extraHeaders != null)
            foreach (var kv in extraHeaders)
                headers[kv.Key] = kv.Value;

        var baseString = GenerateBaseString(requestMethod, requestUrl, headers, requestParams, null, null, extraHeaders, prepend);
        string signature;
        if (signatureMethod == "RSA-SHA256")
        {
            using var privateKey = ReadPrivateKey(config.SignatureKeyPath);
            signature = GenerateRsaSha256Signature(baseString, privateKey);
        }
        else
        {
            if (liveSessionToken == null)
                throw new ArgumentException("Live session token required for HMAC-SHA256 signature");
            signature = GenerateHmacSha256Signature(baseString, liveSessionToken);
        }

        headers["oauth_signature"] = signature;
        var headerString = GenerateAuthorizationHeaderString(headers, config.Realm);
        return new Dictionary<string, string>
        {
            ["Accept"] = "*/*",
            ["Accept-Encoding"] = "gzip,deflate",
            ["Authorization"] = headerString,
            ["Connection"] = "keep-alive",
            ["User-Agent"] = "ibkr-dotnet"
        };
    }

    private static (string Prepend, Dictionary<string,string> ExtraHeaders, string DhRandom) PrepareOauth(OAuth1aConfig config)
    {
        var dhRandom = GenerateDhRandomBytes();
        var dhChallenge = GenerateDhChallenge(config.DhPrime, dhRandom, config.DhGenerator);
        var prepend = CalculateLiveSessionTokenPrepend(config.AccessTokenSecret, ReadPrivateKey(config.EncryptionKeyPath));
        var extra = new Dictionary<string,string> { ["diffie_hellman_challenge"] = dhChallenge };
        return (prepend, extra, dhRandom);
    }

    private static string GenerateRequestTimestamp() => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

    private static RSA ReadPrivateKey(string path)
    {
        using var reader = new StreamReader(path);
        var pem = reader.ReadToEnd();
        var rsa = RSA.Create();
        rsa.ImportFromPem(pem.ToCharArray());
        return rsa;
    }

    private static string GenerateOauthNonce()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        var result = new char[16];
        for (int i = 0; i < 16; i++)
            result[i] = chars[bytes[i] % chars.Length];
        return new string(result);
    }

    private static string GenerateBaseString(
        string method,
        string url,
        Dictionary<string,string> headers,
        Dictionary<string,string>? requestParams,
        Dictionary<string,string>? formData,
        Dictionary<string,string>? body,
        Dictionary<string,string>? extraHeaders,
        string? prepend)
    {
        var allParams = new SortedDictionary<string,string>(headers);
        void Merge(Dictionary<string,string>? d)
        {
            if (d == null) return;
            foreach (var kv in d) allParams[kv.Key] = kv.Value;
        }
        Merge(requestParams); Merge(formData); Merge(body); Merge(extraHeaders);
        var paramString = string.Join("&", allParams.Select(kv => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));
        var baseComponents = new []{ method.ToUpperInvariant(), WebUtility.UrlEncode(url), WebUtility.UrlEncode(paramString) };
        var baseString = string.Join("&", baseComponents);
        if (!string.IsNullOrEmpty(prepend)) baseString = prepend + baseString;
        return baseString;
    }

    private static string GenerateRsaSha256Signature(string baseString, RSA privateKey)
    {
        var data = Encoding.GetEncoding(EncodingName).GetBytes(baseString);
        var signature = privateKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return WebUtility.UrlEncode(Convert.ToBase64String(signature));
    }

    private static string GenerateHmacSha256Signature(string baseString, string liveSessionToken)
    {
        var key = Convert.FromBase64String(liveSessionToken);
        using var hmac = new HMACSHA256(key);
        var data = Encoding.GetEncoding(EncodingName).GetBytes(baseString);
        return WebUtility.UrlEncode(Convert.ToBase64String(hmac.ComputeHash(data)));
    }

    private static string GenerateDhRandomBytes()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GenerateDhChallenge(string dhPrime, string dhRandom, int generator)
    {
        var dhRandomInt = BigInteger.Parse("00" + dhRandom, System.Globalization.NumberStyles.HexNumber);
        var dhPrimeInt = BigInteger.Parse("00" + dhPrime, System.Globalization.NumberStyles.HexNumber);
        var dh = BigInteger.ModPow(new BigInteger(generator), dhRandomInt, dhPrimeInt);
        return dh.ToString("x");
    }

    private static string CalculateLiveSessionTokenPrepend(string accessTokenSecret, RSA privateKey)
    {
        var secretBytes = Convert.FromBase64String(accessTokenSecret);
        var decrypted = privateKey.Decrypt(secretBytes, RSAEncryptionPadding.Pkcs1);
        return BitConverter.ToString(decrypted).Replace("-", string.Empty).ToLowerInvariant();
    }

    private static string CalculateLiveSessionToken(string dhPrime, string dhRandom, string dhResponse, string prepend)
    {
        var dhRandomInt = BigInteger.Parse("00" + dhRandom, System.Globalization.NumberStyles.HexNumber);
        var dhResponseInt = BigInteger.Parse("00" + dhResponse, System.Globalization.NumberStyles.HexNumber);
        var dhPrimeInt = BigInteger.Parse("00" + dhPrime, System.Globalization.NumberStyles.HexNumber);
        var sharedSecret = BigInteger.ModPow(dhResponseInt, dhRandomInt, dhPrimeInt);
        var key = ToByteArray(sharedSecret);
        using var hmac = new HMACSHA1(key);
        var accessBytes = HexToBytes(prepend);
        var hash = hmac.ComputeHash(accessBytes);
        return Convert.ToBase64String(hash);
    }

    private static byte[] HexToBytes(string hex)
    {
        if (hex.Length % 2 == 1) hex = "0" + hex;
        var bytes = new byte[hex.Length / 2];
        for (int i=0;i<bytes.Length;i++)
            bytes[i] = Convert.ToByte(hex.Substring(i*2,2), 16);
        return bytes;
    }

    private static byte[] ToByteArray(BigInteger x)
    {
        var hex = x.ToString("x");
        if (hex.Length % 2 == 1) hex = "0" + hex;
        var list = new List<byte>();
        if (x.GetBitLength() % 8 == 0)
            list.Add(0);
        for (int i=0;i<hex.Length;i+=2)
            list.Add(Convert.ToByte(hex.Substring(i,2),16));
        return list.ToArray();
    }

    private static string GenerateAuthorizationHeaderString(Dictionary<string,string> requestData, string realm)
    {
        var pairs = requestData.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}=\"{kv.Value}\"");
        return $"OAuth realm=\"{realm}\", {string.Join(", ", pairs)}";
    }
}
