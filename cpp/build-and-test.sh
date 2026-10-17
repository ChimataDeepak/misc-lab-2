#!/bin/sh
# Runs inside misc-lab-2/cpp-test:13, which contains the complete build toolchain.
set -e
cmake -S . -B /tmp/build -DCMAKE_BUILD_TYPE=Release >/dev/null
cmake --build /tmp/build -j >/dev/null
ctest --test-dir /tmp/build --output-on-failure
printf '{"id":"evt_1"}' | /tmp/build/misclab-verify --secret whsec_test \
  --header 't=1700000000,v1=c89214b5b5da833daed6f0b8c5bb6bd58cea9022bd80ccc78230f3942d632925' && exit 1 || echo "CLI correctly rejects the 2023 vector as stale"
