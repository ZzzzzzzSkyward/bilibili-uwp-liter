#!/usr/bin/env bash
# C1: the video detail pipeline keeps ugc_season and the detail page renders it.
#
# The detail model lives inside Modules/Detail/VideoDetailVM.cs, which also contains the VM
# and the REST API wiring that cannot compile outside UWP. This script therefore regenerates
# build/DetailModels.cs from the real file every run: everything from
# "public class VideoDetailModel" to the end of the file (that region is plain models), with
# the closing brace of the file's namespace dropped and the needed usings prepended. Editing
# the model in the product source changes what the probe compiles, which is what makes the
# executed assertions meaningful.
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../../.." && pwd)"
vm="$repo/src/BiliLite.UWP/Modules/Detail/VideoDetailVM.cs"
out="$repo/work/improvements/tests/c1/build/DetailModels.cs"

start="$(grep -n '^    public class VideoDetailModel' "$vm" | head -n 1 | cut -d: -f1)"
if [ -z "$start" ]; then
  echo "VideoDetailModel not found in $vm" >&2
  exit 1
fi

mkdir -p "$(dirname -- "$out")"
{
  printf '%s\n' 'using System;' 'using System.Collections.Generic;' 'using System.Linq;' 'using BiliLite.Models.Common;' 'using Newtonsoft.Json;' 'using Newtonsoft.Json.Linq;' '' 'namespace BiliLite.Modules' '{'
  sed -n "$start,\$p" "$vm" | sed '$d'
  printf '%s\n' '}'
} > "$out"

dotnet run --project "$repo/work/improvements/tests/c1/C1Probe.csproj" -c Debug
