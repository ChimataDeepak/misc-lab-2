using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Misclab.Webhooks;
using Xunit;

public class ReceiverTests
{
    private const string Secret = "whsec_test";
    private const long T = 1700000000;
    private const string Body = "{\"id\":\"evt_42\",\"type\":\"payment.succeeded\",\"amountMinor\":1250}";

    private static HttpClient Client(long now) => new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
    {
        b.UseSetting("MISCLAB_WEBHOOK_SECRET", Secret);
        b.ConfigureServices(s => s.AddSingleton(new ReceiverClock(() => now)));
    }).CreateClient();

    private static HttpRequestMessage Post(string body, string? header)
    {
        var m = new HttpRequestMessage(HttpMethod.Post, "/webhooks/payments") { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        if (header != null) m.Headers.Add(WebhookSignature.HeaderName, header);
        return m;
    }

    [Fact]
    public async Task ProcessesOnceThenReportsDuplicate()
    {
        var c = Client(T + 5);
        var h = WebhookSignature.Sign(Secret, T, Encoding.UTF8.GetBytes(Body));
        Assert.Contains("processed", await (await c.SendAsync(Post(Body, h))).Content.ReadAsStringAsync());
        Assert.Contains("duplicate", await (await c.SendAsync(Post(Body, h))).Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TamperedAmountIsUnauthorized()
    {
        var h = WebhookSignature.Sign(Secret, T, Encoding.UTF8.GetBytes(Body));
        var r = await Client(T).SendAsync(Post(Body.Replace("1250", "9999"), h));
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task StaleEventIsUnauthorized()
    {
        var h = WebhookSignature.Sign(Secret, T, Encoding.UTF8.GetBytes(Body));
        var r = await Client(T + 3600).SendAsync(Post(Body, h));
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        Assert.Contains("stale", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task MissingHeaderIsBadRequest() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await Client(T).SendAsync(Post(Body, null))).StatusCode);
}
