#!/bin/sh
# Runs every language's tests in its official/runtime image against shared/test-vector.json.
set -e
ROOT=$(cd "$(dirname "$0")" && pwd)
run() { echo "=== $1 ==="; docker run --rm -v "$ROOT":/w -w "/w/$2" "$3" sh -c "$4"; }
run "Node.js (TypeScript)" node node:20 "npm ci --no-audit --no-fund >/dev/null && npx tsc -p . --noEmit && npx vitest run"
echo "=== C++17 ==="
docker build -q -t misc-lab-2/cpp-test:13 -f "$ROOT/cpp/Dockerfile" "$ROOT/cpp" >/dev/null
run "C++17 tests" cpp misc-lab-2/cpp-test:13 "./build-and-test.sh"
run "C# (.NET 8)" csharp mcr.microsoft.com/dotnet/sdk:8.0 "dotnet test tests/Misclab.Webhooks.Tests"
run "Ruby" ruby ruby:3.3 "bundle config set --local path /tmp/bundle >/dev/null && bundle install --quiet && bundle exec rspec"
echo "ALL LANGUAGES PASSED"
