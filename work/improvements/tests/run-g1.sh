#!/usr/bin/env bash
# G1: app-key-aware gRPC headers. Reflection probe over the built BiliLite.gRPC assembly.
set -euo pipefail

cd "$(dirname "$0")/../../.."
dotnet run --project work/improvements/tests/g1/G1Probe.csproj -c Debug
