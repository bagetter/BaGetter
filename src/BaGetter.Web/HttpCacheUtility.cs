using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace BaGetter.Web;

internal static class HttpCacheUtility
{
    public static string CreateStrongEtagFromBytes(ReadOnlySpan<byte> bytes)
    {
        var hash = SHA256.HashData(bytes);
        return $"\"{Convert.ToHexString(hash)}\"";
    }

    public static string CreateStrongEtagFromText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return CreateStrongEtagFromBytes(Encoding.UTF8.GetBytes(text));
    }

    public static string CreateStrongEtagFromParts(params string[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        return CreateStrongEtagFromText(string.Join('|', parts.Select(part => part ?? string.Empty)));
    }

    public static bool MatchesIfNoneMatch(HttpRequest request, string etag)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(etag);

        var header = request.Headers.IfNoneMatch.ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            return false;
        }

        var normalizedCurrent = NormalizeEtag(etag);
        var candidates = header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var candidate in candidates)
        {
            if (candidate == "*")
            {
                return true;
            }

            if (NormalizeEtag(candidate).Equals(normalizedCurrent, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static void SetEtag(HttpResponse response, string etag)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(etag);
        response.Headers[HeaderNames.ETag] = etag;
    }

    public static void SetMustRevalidate(HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.Headers[HeaderNames.CacheControl] = "public, max-age=0, must-revalidate";
    }

    public static void SetImmutable(HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000, immutable";
    }

    private static string NormalizeEtag(string etag)
    {
        var trimmed = etag.Trim();
        if (trimmed.StartsWith("W/", StringComparison.Ordinal))
        {
            trimmed = trimmed[2..].Trim();
        }

        return trimmed;
    }
}
