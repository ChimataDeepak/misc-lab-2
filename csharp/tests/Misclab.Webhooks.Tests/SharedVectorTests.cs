using System.Text;
using System.Text.Json;
using Misclab.Webhooks;
using Xunit;

public class SharedVectorTests
{
    private static readonly JsonElement V = JsonDocument.Parse(File.ReadAllText(FindVector())).RootElement;

    internal static string FindVector()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
        {
            var p = Path.Combine(d.FullName, "shared", "test-vector.json");
            if (File.Exists(p)) return p;
        }
        throw new FileNotFoundException("shared/test-vector.json");
    }

    [Fact]
    public void SignReproducesJavaAndGoHeader() =>
        Assert.Equal(V.GetProperty("header").GetString(),
            WebhookSignature.Sign(V.GetProperty("secret").GetString()!, V.GetProperty("timestamp").GetInt64(),
                Encoding.UTF8.GetBytes(V.GetProperty("body").GetString()!)));

    public static IEnumerable<object[]> Cases() =>
        V.GetProperty("cases").EnumerateArray().Select(c => new object[] { c.GetProperty("name").GetString()! });

    [Theory]
    [MemberData(nameof(Cases))]
    public void CaseMatchesExpectation(string name)
    {
        var c = V.GetProperty("cases").EnumerateArray().Single(x => x.GetProperty("name").GetString() == name);
        var r = WebhookSignature.Verify(c.GetProperty("secret").GetString()!, c.GetProperty("header").GetString(),
            Encoding.UTF8.GetBytes(c.GetProperty("body").GetString()!), c.GetProperty("now").GetInt64(),
            V.GetProperty("toleranceSeconds").GetInt64());
        Assert.Equal(c.GetProperty("expect").GetString(), r.ToString().ToLowerInvariant());
    }

    [Fact]
    public void NullHeaderIsMalformed() =>
        Assert.Equal(VerifyResult.Malformed, WebhookSignature.Verify("s", null, Array.Empty<byte>(), 0));
}
