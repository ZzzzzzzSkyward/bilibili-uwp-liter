#!/usr/bin/env bash
# Ux0: the Watch Later queue opens the row that was clicked and the playlist count never lies.
#
# The probe compiles the real Models/VideoCollections.cs (VideoPlaylistOrder, the queue-position
# resolver the page uses) and Models/MediaListResources.cs (MediaListPagingState, its cursor,
# duplicate suppression and total count), so editing those decisions in the product changes what
# the executed assertions see. The pages cannot run outside UWP; their wiring is read as text and
# every such check is named "source:".
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"

dotnet run --project "$repo/plan/tests/ux0/Ux0Probe.csproj" -c Debug
