#!/usr/bin/env bash
# Ux1b: the 目录 directory is one entry in every viewing mode — the same backing state in the detail
# column and in a player drawer, built by one pure model.
#
# The probe compiles the real Models/PlayDirectory.cs (rows, sections, captions, count honesty and
# identity) with Models/VideoQueue.cs, Models/MediaListResources.cs and Models/VideoCollections.cs,
# so editing those decisions changes what the executed assertions see. The player, the page and the
# control cannot run outside UWP; their wiring is read as text and every such check is named
# "source:".
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"

dotnet run --project "$repo/plan/tests/ux1b/Ux1bProbe.csproj" -c Debug
