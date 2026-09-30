# Run results

Date: 2026-09-30. Host: macOS arm64, Rancher Desktop Docker. Command: `./run-all.sh` (each language in its official image).

| Language | Image | Command | Result |
|----------|-------|---------|--------|
| Node.js / TypeScript | `node:20` | `npm ci && npx tsc --noEmit && npx vitest run` | 2 files, **14 passed**, 0 failed (typecheck clean) |
| C++17 | `gcc:13` (+ apt cmake, libssl-dev, libgtest-dev) | `./build-and-test.sh` (cmake, `-Werror`, ctest, CLI smoke) | **5/5 passed** (vector test covers all 9 cases); CLI prints `REJECTED: stale` for the 2023 vector as expected |
| C# / .NET 8 | `mcr.microsoft.com/dotnet/sdk:8.0` | `dotnet test tests/Misclab.Webhooks.Tests` | **15 passed**, 0 failed, 0 skipped |
| Ruby 3.3 | `ruby:3.3` | `bundle install && bundle exec rspec` | **15 examples, 0 failures** |

Total: 49 tests, 0 failures. Known-good header `t=1700000000,v1=c89214b5...2925` reproduced in all four languages
(also cross-checked with Python `hmac` on the host). GitHub Actions matrix is written but has not run (branch not pushed).
