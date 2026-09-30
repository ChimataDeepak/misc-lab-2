// Misclab webhook signatures: "t=<unix>,v1=<hex HMAC-SHA256(secret, "<t>." + body)>".
using System.Security.Cryptography;
using System.Text;

namespace Misclab.Webhooks;

public enum VerifyResult { Ok, Malformed, Mismatch, Stale }

public static class WebhookSignature
{
    public const string HeaderName = "Misclab-Signature";

    public static string Sign(string secret, long t, ReadOnlySpan<byte> body) => $"t={t},v1={MacHex(secret, t, body)}";

    public static (long T, string V1)? ParseHeader(string? header)
    {
        if (string.IsNullOrEmpty(header)) return null;
        long t = -1; string v1 = "";
        foreach (var raw in header.Split(','))
        {
            var part = raw.Trim();
            var i = part.IndexOf('=');
            if (i < 0) continue;
            var (k, v) = (part[..i], part[(i + 1)..]);
            if (k == "t" && v.All(char.IsAsciiDigit) && long.TryParse(v, out var n)) t = n;
            else if (k == "v1") v1 = v;
        }
        return t < 0 || v1.Length == 0 ? null : (t, v1);
    }

    /// Constant-time signature comparison first, then the replay window |now - t| &lt;= tolerance.
    public static VerifyResult Verify(string secret, string? header, ReadOnlySpan<byte> body, long nowSec, long toleranceSec = 300)
    {
        if (ParseHeader(header) is not var (t, v1)) return VerifyResult.Malformed;
        var expected = Encoding.ASCII.GetBytes(MacHex(secret, t, body));
        var given = Encoding.ASCII.GetBytes(v1);
        if (!CryptographicOperations.FixedTimeEquals(expected, given)) return VerifyResult.Mismatch;
        return Math.Abs(nowSec - t) > toleranceSec ? VerifyResult.Stale : VerifyResult.Ok;
    }

    private static string MacHex(string secret, long t, ReadOnlySpan<byte> body)
    {
        using var h = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(secret));
        h.AppendData(Encoding.ASCII.GetBytes($"{t}."));
        h.AppendData(body);
        return Convert.ToHexString(h.GetHashAndReset()).ToLowerInvariant();
    }
}
