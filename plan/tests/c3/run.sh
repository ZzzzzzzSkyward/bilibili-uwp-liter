#!/usr/bin/env bash
# C3: remote playlist links parse, the queue advances across pages and collections order plays.
#
# The probe compiles the real Models/MediaListResources.cs (route parser + queue decision +
# paging state) and Models/VideoCollections.cs (collection order), so editing those decisions
# in the product changes what the executed assertions see. Page and MessageCenter wiring
# cannot run outside UWP; those checks are read as text and named "source:".
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"

dotnet run --project "$repo/plan/tests/c3/C3Probe.csproj" -c Debug
