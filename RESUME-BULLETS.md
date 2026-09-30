# Resume bullets (verified items only; all tested locally in Docker containers)

- Implemented HMAC-SHA256 payment-webhook signature verification SDKs in TypeScript, C++17, C# (.NET 8) and Ruby, all passing one shared cross-language test vector that also matches the existing Java signer and Go verifier (49 tests, run in Docker).
- Built signature-verifying webhook receivers in Express, ASP.NET Core minimal APIs and Rack with constant-time comparison, a 5-minute replay window and event-id deduplication, covered by in-memory HTTP tests (supertest, WebApplicationFactory, rack-test).
- Wrote a C++17 verifier library and CLI on OpenSSL, built with CMake under `-Werror` and tested with GoogleTest/ctest inside the official gcc image.
- Added a one-command Docker test runner and a GitHub Actions matrix (written, not yet run) across four language toolchains.
