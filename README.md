# misc-lab-2: polyglot payments SDKs

The misc-lab payments module (Java, branch `miscellaneous`) signs every outgoing payment webhook with
`Misclab-Signature: t=<unix seconds>,v1=<hex HMAC-SHA256(secret, "<t>." + rawBody)>`, and `go-verify` checks it in Go.
This branch adds the same verifier as small, idiomatic SDKs in four more languages, each with a tiny receiver service,
and **all of them pass one shared test vector**: `shared/test-vector.json` (the same vector the Java and Go tests use).

| Dir | Language / stack | What is in it | Tests |
|-----|------------------|---------------|-------|
| `node/` | TypeScript, Node 20, Express | `sign`/`verify` SDK (`crypto.timingSafeEqual`), Express receiver with raw-body verification, replay window, event-id dedupe | vitest + supertest, 14 |
| `cpp/` | C++17, CMake, OpenSSL | `libmisclab_verify` (OpenSSL `HMAC`, `CRYPTO_memcmp`), `misclab-verify` CLI (exit 0/1/2), built with `-Werror` | GoogleTest via ctest, 5 (one runs all 9 vector cases) |
| `csharp/` | C#, .NET 8 | `Misclab.Webhooks` library (`IncrementalHash`, `CryptographicOperations.FixedTimeEquals`), minimal-API receiver | xUnit + `WebApplicationFactory`, 15 |
| `ruby/` | Ruby 3.3, Rack | gem-style `Misclab::WebhookSignature` (`OpenSSL.fixed_length_secure_compare`), Rack receiver (`config.ru`) with a mutex-guarded dedupe set | RSpec + rack-test, 15 |

Run everything (Docker only, nothing installed on the host): `./run-all.sh`. CI: `.github/workflows/ci.yml` runs the same four commands as a matrix.
Results: [RUN-RESULTS.md](RUN-RESULTS.md).

## Receiver behaviour (identical in all three services)

`POST /webhooks/payments` with the raw body and a `Misclab-Signature` header:
400 malformed or missing header, 401 `mismatch` (tampered body / wrong secret / moved timestamp), 401 `stale` (outside +/-300 s),
200 `processed` the first time an event id is seen, 200 `duplicate` afterwards (at-least-once delivery means retries are normal).

## How I'd explain it in an interview

- **HMAC-SHA256 over `"<t>." + body`** — the timestamp is inside the MAC, so an attacker cannot take an old valid signature and
  attach a fresh timestamp ("moved timestamp" case fails). A plain SHA-256 hash would not prove who sent it; HMAC needs the shared secret.
- **Constant-time comparison** — `==` on strings returns at the first differing byte, so response timing leaks how much of a
  forged signature was right. Every SDK uses the platform's constant-time primitive (`timingSafeEqual`, `CRYPTO_memcmp`,
  `FixedTimeEquals`, `fixed_length_secure_compare`) and checks length first.
- **Replay window** — a captured valid request can be resent. Rejecting `|now - t| > 300 s` bounds that window; event-id dedupe
  covers replays inside it and also makes the receiver idempotent for legitimate retries.
- **Verify the raw bytes** — every receiver reads the body before any JSON parsing. Re-serializing JSON changes whitespace or key
  order and breaks the signature.
- **Why one shared test vector** — five implementations only interoperate if they agree byte for byte. A single JSON file with the
  known-good header plus 9 negative/edge cases (including exactly-300 s = ok, 301 s = stale) is the contract; each language's tests
  load that file instead of copying constants, so the file changing breaks everyone at once.
- **Per-language notes** — C++: RAII-free one-shot `HMAC()` and a CLI with meaningful exit codes; C#: `WebApplicationFactory` for
  in-memory HTTP tests with the clock injected through DI; Node: `express.raw` so the body stays a Buffer; Ruby: plain Rack app,
  thread-safe dedupe with a `Mutex`.

## Limits (honest)

In-memory dedupe only (a real receiver would use a DB unique key or Redis `SET NX` with TTL > tolerance). Single `v1` signature;
no secret rotation (Stripe-style multiple `v1=` entries) yet. Tested locally in Docker containers, not deployed.
