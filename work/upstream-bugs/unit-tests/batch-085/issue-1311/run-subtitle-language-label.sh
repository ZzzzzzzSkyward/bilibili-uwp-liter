#!/usr/bin/env bash
set -euo pipefail
here="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo="$(cd -- "$here/../../../../.." && pwd)"
tools_root="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
python3 - "$repo/src/BiliLite.UWP/Controls/PlayerControl.xaml.cs" "$repo/src/BiliLite.UWP/Dialogs/DownloadDialog.xaml.cs" "$here/probe-template.csx" "$here/probe.csx" <<'PY'
import sys
player = open(sys.argv[1], encoding='utf-8-sig').read()
dialog = open(sys.argv[2], encoding='utf-8-sig').read()
template = open(sys.argv[3], encoding='utf-8-sig').read()
api = next(line.strip() for line in player.splitlines() if 'ToggleMenuFlyoutItem menuitem =' in line and 'item.subtitle_url' in line)
lines = dialog.splitlines()
name_index = next(index for index, line in enumerate(lines) if 'Name =' in line and 'subtitleItem.lan_doc' in line)
download = lines[name_index].strip().rstrip(',') + ', ' + lines[name_index + 1].strip().rstrip(',')
selection = next(line.strip() for line in player.splitlines() if 'var selectNone =' in line and 'firstMenuItem.Text' in line)
assert 'item.lan_doc}({item.lan})' in api, api
assert 'subtitleItem.lan_doc}({subtitleItem.lan})' in download, download
initializer = 'new ToggleMenuFlyoutItem() {'
template = template.replace('__API_LABEL__', api[api.index(initializer) + len(initializer):api.rfind('};')].strip())
template = template.replace('__DOWNLOAD_LABEL__', download)
template = template.replace('__SELECT_NONE__', selection)
open(sys.argv[4], 'w').write(template)
PY
export DOTNET_ROOT="$tools_root/dotnet"
export DOTNET_CLI_HOME="$tools_root/cache/dotnet-cli"
export NUGET_PACKAGES="$tools_root/cache/nuget"
export XDG_CACHE_HOME="$tools_root/cache/xdg"
export XDG_DATA_HOME="$tools_root/cache/xdg-data"
exec "$DOTNET_ROOT/dotnet" "$tools_root/tool/dotnet-script-1.6.0/tools/net8.0/any/dotnet-script.dll" --no-cache "$here/probe.csx"
