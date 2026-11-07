#!/usr/bin/env bash
# Ux3: 剧集页的目录是正片、预告两段,谁也不混进谁;队列带着用户认得的名字到抽屉页头。
#
# Executed against product source: Models/PlayDirectory.cs (the season sections, their identity and
# current-row rules, the browsed-section header, the footer rule and the queue title rule). The
# pages, the player and the drawer cannot run outside UWP, so their wiring is read as text and every
# such check is named "source:".
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"

dotnet run --project "$repo/plan/tests/ux3/Ux3Probe.csproj" -c Debug
