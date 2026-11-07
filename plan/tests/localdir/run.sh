#!/usr/bin/env bash
# Localdir: 本地播放页把下载/合集的多集交给播放器的目录。
#
# Executed against product source: Models/PlayDirectory.cs (the directory builder runs for real).
# The page cannot run outside UWP, so its wiring is read as text and every such check is named
# "source:". --legacy runs the same page anchors against the pre-change page body and must be red.
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"

dotnet run --project "$repo/plan/tests/localdir/LocalDirProbe.csproj" -c Debug -- "$@"
