// Minimal API receiver: verify raw-body signature + replay window, then dedupe by event id.
using System.Collections.Concurrent;
using System.Text.Json;
using Misclab.Webhooks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ReceiverClock>(_ => new ReceiverClock(() => DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
builder.Services.AddSingleton<ProcessedEvents>();
var app = builder.Build();

app.MapPost("/webhooks/payments", async (HttpRequest req, ReceiverClock clock, ProcessedEvents store, IConfiguration cfg) =>
{
    var secret = cfg["MISCLAB_WEBHOOK_SECRET"] ?? throw new InvalidOperationException("MISCLAB_WEBHOOK_SECRET required");
    using var ms = new MemoryStream();
    await req.Body.CopyToAsync(ms);          // exact bytes: the HMAC is over what was sent, not re-serialized JSON
    var body = ms.ToArray();
    var result = WebhookSignature.Verify(secret, req.Headers[WebhookSignature.HeaderName], body, clock.Now());
    if (result != VerifyResult.Ok)
        return Results.Json(new { error = result.ToString().ToLowerInvariant() }, statusCode: result == VerifyResult.Malformed ? 400 : 401);
    string? id;
    try { id = JsonDocument.Parse(body).RootElement.TryGetProperty("id", out var p) ? p.GetString() : null; }
    catch (JsonException) { return Results.BadRequest(new { error = "bad json" }); }
    if (string.IsNullOrEmpty(id)) return Results.BadRequest(new { error = "missing id" });
    return Results.Ok(new { status = store.TryAdd(id) ? "processed" : "duplicate" });
});

app.Run();

public sealed record ReceiverClock(Func<long> Now);
public sealed class ProcessedEvents
{
    private readonly ConcurrentDictionary<string, byte> _seen = new();
    public bool TryAdd(string id) => _seen.TryAdd(id, 0);
    public int Count => _seen.Count;
}
public partial class Program { }
