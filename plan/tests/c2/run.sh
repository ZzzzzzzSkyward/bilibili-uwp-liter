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

# Bind the complete unchanged production decoder body, including salvage and legacy methods.
python3 - "$repo" <<'PYDECODER'
from pathlib import Path
import sys
repo = Path(sys.argv[1])
source = (repo / "src/BiliLite.UWP/Helpers/HttpHelper.cs").read_text(encoding="utf-8-sig")
marker = "    public class HttpResults\n"
assert source.count(marker) == 1, "HttpResults marker must be unique"
assert source.endswith("    }\n}\n"), "HttpResults must remain the final class"
body = source[source.index(marker):source.rfind("}")]
out = repo / "plan/tests/c2/obj/HttpResults.actual.cs"
out.parent.mkdir(exist_ok=True)
out.write_text("using System;\nusing System.Threading.Tasks;\nusing BiliLite.Models;\nusing Newtonsoft.Json;\nusing Newtonsoft.Json.Linq;\nnamespace BiliLite.Helpers {\n" + body + "\n}\n")
PYDECODER

"$dotnet" run --project "$probe" -c Debug
