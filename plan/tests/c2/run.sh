#!/usr/bin/env bash
# C2: remote medialist playlists parse, page and de-duplicate correctly.
#
# The probe compiles the real Models/MediaListResources.cs and Services/MediaListService.cs, so
# editing the parsing, the cursor rule or the retry trigger in the product changes what the
# executed assertions see. The service's requests are answered from canned
# /x/v2/medialist/resource/list payloads instead of the network.
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"

dotnet run --project "$repo/plan/tests/c2/C2Probe.csproj" -c Debug
