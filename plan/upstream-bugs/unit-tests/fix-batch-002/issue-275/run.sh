#!/usr/bin/env bash
set -euo pipefail
here="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo="$(cd -- "$here/../../../.." && pwd)"
tools_root="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
sed -n '201,225p' "$repo/src/BiliLite.UWP/Controls/CommentControl.xaml.cs" > "$here/source-fragment.csx"
{ cat "$here/header.csx"; printf '{\n'; cat "$here/source-fragment.csx"; printf '}\n'; cat "$here/middle.csx"; printf '{\n'; cat "$here/source-fragment.csx"; printf '}\n'; cat "$here/footer.csx"; } > "$here/test.csx"
export DOTNET_ROOT="$tools_root/dotnet" DOTNET_CLI_HOME="$tools_root/cache/dotnet-cli" NUGET_PACKAGES="$tools_root/cache/nuget" XDG_CACHE_HOME="$tools_root/cache/xdg" XDG_DATA_HOME="$tools_root/cache/xdg-data"
exec "$DOTNET_ROOT/dotnet" "$tools_root/tool/dotnet-script-1.6.0/tools/net8.0/any/dotnet-script.dll" --no-cache "$here/test.csx"
