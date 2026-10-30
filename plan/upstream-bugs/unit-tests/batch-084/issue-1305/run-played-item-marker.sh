#!/usr/bin/env bash
set -euo pipefail
here="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo="$(cd -- "$here/../../../.." && pwd)"
tools_root="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
python3 - "$repo/src/BiliLite.UWP/Controls/PlayerControl.xaml.cs" "$here/probe-template.csx" "$here/probe.csx" <<'PY'
import sys
source = open(sys.argv[1], encoding='utf-8-sig').read()
template = open(sys.argv[2], encoding='utf-8-sig').read()
def block_after(anchor):
    start = source.index(anchor)
    brace = source.index('{', start)
    depth = 0
    for end in range(brace, len(source)):
        if source[end] == '{':
            depth += 1
        elif source[end] == '}':
            depth -= 1
            if depth == 0:
                return source[start:end + 1]
    raise AssertionError(f'unclosed block: {anchor}')
identity = block_after('if (CurrentPlayItem == null || CurrentPlayItem.season_id')
playing = block_after('if (e == PlayState.Playing)')
assert '_hasPlayedCurrentItem = false;' in identity
assert '_hasPlayedCurrentItem = true;' in playing
template = template.replace('__IDENTITY_RESET__', identity)
template = template.replace('__PLAYING_MARKER__', playing)
open(sys.argv[3], 'w').write(template)
PY
export DOTNET_ROOT="$tools_root/dotnet"
export DOTNET_CLI_HOME="$tools_root/cache/dotnet-cli"
export NUGET_PACKAGES="$tools_root/cache/nuget"
export XDG_CACHE_HOME="$tools_root/cache/xdg"
export XDG_DATA_HOME="$tools_root/cache/xdg-data"
exec "$DOTNET_ROOT/dotnet" "$tools_root/tool/dotnet-script-1.6.0/tools/net8.0/any/dotnet-script.dll" --no-cache "$here/probe.csx"
