#!/usr/bin/env bash
# C2: remote medialist playlists parse, page and de-duplicate correctly.
#
# The probe compiles the real Models/MediaListResources.cs and Services/MediaListService.cs, so
# editing the parsing, the cursor rule or the retry trigger in the product changes what the
# executed assertions see. The service's requests are answered from canned
# /x/v2/medialist/resource/list payloads instead of the network.
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
probe="$repo/plan/tests/c2/C2Probe.csproj"

portable="${UPSTREAM_UNIT_TOOLS:-/nonexistent}/dotnet"
if [ -f "$portable/dotnet" ]; then
    export DOTNET_ROOT="$portable"
    dotnet="$portable/dotnet"
elif [ -f "$portable/dotnet.exe" ]; then
    export DOTNET_ROOT="$portable"
    dotnet="$portable/dotnet.exe"
elif [ -x "C:/Program Files/dotnet/dotnet.exe" ]; then
    dotnet="C:/Program Files/dotnet/dotnet.exe"
else
    dotnet="dotnet"
fi

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

"$dotnet" run --project "$probe" -c Debug
