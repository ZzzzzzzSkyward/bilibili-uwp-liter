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

# Compile the complete actual async Task.Run JSON helper, never a probe implementation.
python3 - "$repo" <<'PYDESERIALIZER'
from pathlib import Path
import re
import sys
repo = Path(sys.argv[1])
source = (repo / "src/BiliLite.UWP/Helpers/Utils.cs").read_text(encoding="utf-8-sig")
marker = "        public static async Task<T> DeserializeJson<T>(this string results)\n"
end_marker = "        public static string ToSimplifiedChinese(string content)\n"
assert len(re.findall(r"^\s*public\s+static\s+async\s+Task<T>\s+DeserializeJson<T>\s*\(", source, re.M)) == 1, "DeserializeJson declaration must be unique"
assert source.count(marker) == 1 and source.count(end_marker) == 1, "DeserializeJson boundaries must be unique"
start = source.index(marker)
end = source.index(end_marker)
assert end > start, "DeserializeJson boundaries must be ordered"
method = source[start:end]
assert method.startswith(marker + "        {\n"), "DeserializeJson opening boundary is incomplete"
assert method.endswith("            });\n        }\n"), "DeserializeJson closing boundary is incomplete"
assert method.count("{") == method.count("}") == 2, "DeserializeJson method/lambda bodies must be complete"
assert method.count("return await Task.Run<T>(() =>") == 1, "DeserializeJson must retain its actual async Task.Run boundary"
out = repo / "plan/tests/c2/obj/Utils.DeserializeJson.actual.cs"
out.parent.mkdir(exist_ok=True)
out.write_text("using System.Threading.Tasks;\nusing Newtonsoft.Json;\nnamespace BiliLite.Helpers {\n    public static partial class Utils\n    {\n" + method + "    }\n}\n")
PYDESERIALIZER

"$dotnet" run --project "$probe" -c Debug
