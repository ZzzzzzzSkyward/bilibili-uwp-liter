#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../../.." && pwd)"
# Superseded the fragile fixed-line fragment extraction with the actual-source API-01 probe.
# The probe compiles CookieService and ApiHelper, then verifies confirm-refresh POST dispatch offline.
exec "$repo_root/plan/tests/run-api01.sh"
