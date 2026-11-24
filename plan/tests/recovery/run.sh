#!/usr/bin/env bash
set -euo pipefail
probe="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
python3 "$probe/prepare-source.py"
dotnet run --project "$probe/RecoveryProbe.csproj" -c Debug
