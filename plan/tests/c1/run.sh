#!/usr/bin/env bash
# C1 compiles the complete actual VideoDetailVM; only request delivery/platform primitives are stubs.
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
generated="$repo/plan/tests/c1/obj/probe-generated"
mkdir -p "$generated"
python3 - "$repo" "$generated" <<'EXTRACT'
from pathlib import Path
import sys,hashlib,json,re
repo=Path(sys.argv[1]); out=Path(sys.argv[2]); source=repo/'src/BiliLite.UWP/Helpers/HttpHelper.cs'
raw=source.read_bytes(); markers=list(re.finditer(rb'^    public class HttpResults\r?$', raw, re.MULTILINE))
assert len(markers)==1, 'HttpResults start marker is not unique'
body=raw[markers[0].start():]
assert body.rstrip().endswith(b'}'), 'HttpResults namespace closure missing'
# The entire class and final namespace closure are copied verbatim, including production salvage.
prefix='using System;\nusing System.Threading.Tasks;\nusing Newtonsoft.Json;\nusing Newtonsoft.Json.Linq;\nusing BiliLite.Models;\nnamespace BiliLite.Helpers\n{\n'
prefix=prefix.encode()
(out/'HttpResults.cs').write_bytes(prefix+body)
assert (out/'HttpResults.cs').read_bytes()[len(prefix):]==body
(out/'extraction-provenance.json').write_text(json.dumps({'source':str(source.relative_to(repo)),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'body_sha256':hashlib.sha256(body).hexdigest()},indent=2)+'\n')
EXTRACT
dotnet run --project "$repo/plan/tests/c1/C1Probe.csproj" -c Debug
