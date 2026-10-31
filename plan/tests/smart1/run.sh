#!/usr/bin/env bash
# SMART1: adaptive dynamic API selector (batch 1) — structured outcomes + selection core.
#
# The probe compiles the real src/BiliLite.UWP/Services/DynamicApiSelector.cs and the real
# src/BiliLite.gRPC project, so editing the thresholds, the cooldown ladder, the trial
# reservation or the outcome classification in the product changes what the executed
# assertions see. Time is injected, so every scenario is deterministic.
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"

dotnet run --project "$repo/plan/tests/smart1/Smart1Probe.csproj" -c Debug
