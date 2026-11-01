#!/usr/bin/env bash
# C3: remote playlist links parse, the queue advances across pages and collections order plays.
#
# The probe compiles the real Models/MediaListResources.cs (route parser + queue decision +
# paging state) and Models/VideoCollections.cs (collection order), so editing those decisions
# in the product changes what the executed assertions see. Page and MessageCenter wiring
# cannot run outside UWP; those checks are read as text and named "source:".
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
probe="$repo/plan/tests/c3/C3Probe.csproj"

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
