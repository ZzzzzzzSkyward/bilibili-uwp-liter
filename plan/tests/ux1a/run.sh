#!/usr/bin/env bash
# Ux1a: one page-owned sequence decides the next/previous target, and the completion event, the
# next button and the next shortcut all resolve through it.
#
# The probe compiles the real Models/WatchSequence.cs (WatchSequenceOrder's decision table and
# WatchSequenceState) together with Models/MediaListResources.cs (MediaListQueue, the row/paging
# rule the sequence delegates to), so editing those decisions changes what the executed
# assertions see. Pages/VideoDetailPage and Controls/PlayerControl cannot run outside UWP; their
# wiring is read as text and every such check is named "source:".
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"

dotnet run --project "$repo/plan/tests/ux1a/Ux1aProbe.csproj" -c Debug
