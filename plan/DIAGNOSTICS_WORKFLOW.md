# Response-evidence workflow (design)

Two bugs in a row were diagnosed blind and needed a second round because the log carried the verdict but not
the payload:

- BR-17: the log said `hasMore=False offset=` for `dynamic_new` and `dynamic_history 404`, but nothing said what
  the server actually returned. Two builds later the real answer was `feed/all` works and returns items.
- BR-18: the mapper threw `Cannot access child value on Newtonsoft.Json.Linq.JValue`, and the stack named the
  function but not the *field* or the token that was not an object. The fix needs the shape, not the value.

So the goal is not "log more". It is: **every failure leaves behind the smallest artifact that answers the
questions an LLM can only guess at otherwise** — what came back, in what shape, and which reader assumed
otherwise — while the tester only has to click one button.

## What exists today (keep it)

- `Helpers/LogHelper.cs` — NLog text log per day in `Local\BiliLiteUWPLiter…\LocalState\log\yyyyMMdd.log`.
- `Helpers/HttpHelper.cs` — every call gets an id (`http-000012`), the failures log endpoint + status + duration,
  query strings and bodies are already excluded on purpose.
- `gRPC.GrpcRequest.DiagnosticLogger` — a callback the app wires into `LogHelper` for transport-level messages.
- Settings → Debug: **Mark problem** (timestamp + note + session id) and **Export diagnostic logs** (session header +
  the seven newest daily files), documented in `document/DIAGNOSTICS.md`.

What is missing is the middle layer: the response *shape*, the assumption it violated, and a way to turn a captured
response into a permanent probe fixture.

## Design

### 1. Capture record (NDJSON, one line per response)

Path: `LocalState\log\api\yyyyMMdd.ndjson`, one JSON object per line — greppable by a human, parseable by a script,
and cheap for an LLM to read after a `jq`-style filter.

```json
{"v":1,"t":"2026-10-31T08:20:12.004Z","id":"http-000412","ep":"polymer/feed-all","type":"all",
 "status":200,"code":0,"ms":138,"bytes":48213,"app":"1.0.9.15",
 "shape":"d{code:number,data:d{items:[d{id_str:string,type:string,modules:d{module_author:d{...}}}]}}",
 "drift":["d{code:object}","data.items[]:number"],"sample":"sha1:6f3c…","trunc":4096}
```

- `ep` is a **stable logical name** resolved from the URL (`polymer/feed-all`, `legacy/dynamic-new`,
  `grpc/DynAll`, `playurl`), so a log is readable without decoding query strings.
- `shape` is a type tree, not a body: arrays collapse to one element, objects list their keys in sorted order,
  depth-limited (8) and key-limited (64/key). This is what tells a reader *why* a mapper failed.
- `drift` lists the pointers where the shape disagrees with the stored baseline (see §2) — that is the line that
  would have said `rich_text_nodes[].emoji: object→string` on 2026-10-31.
- `sample` is an optional truncated body slice (see §3), off by default.
- `trunc` records how much of the body the shape tree was built from, so a `shape` is never mistaken for the whole
  response.

### 2. Baseline learning and drift (no per-endpoint authoring)

On the first successful response for `ep` (per app version), store the shape in
`LocalState\log\api\baseline.json`. Every later response is diffed against it and only the diff is logged.

- No schema files to maintain, no code churn per endpoint.
- The first response after an app version change re-establishes the baseline — server drift is not confused with a
  broken install.
- The diff is exactly the thing that breaks mappers, and it appears *at capture time*, before the UI falls over.

Add explicit `must be object/array` declarations only where a mapper genuinely depends on it — the web dynamic path
is one; keep that table next to `Helpers/DynamicGrpcMapper.cs` rather than in the logger.

gRPC gets the same treatment for free: `GrpcRequest` knows the reply's `IMessage.Descriptor`, so the shape is
"field name → wire type + populated/absent", and drift is a missing field that a mapper requires.

### 3. Body samples: opt-in, bounded, redacted

- Toggle in Settings → Debug ("采样原始响应"), default **off**, auto-expires after 24 h.
- Cap per response 4 KB, per day 512 KB, 7 days rotation, total under 4 MB.
- Redaction before anything is written: query strings and `Cookie`/`Authorization`/`access_key`/`SESSDATA`/`bili_jct`/
  `w_rid`/`wts` are dropped, and any key whose name matches `token|cookie|csrf|key|sign|secret` has its value
  replaced by `"<redacted:len=N>"`. Stream URLs are truncated to origin + path.
- The bundle never contains a full body: only the slice a failing stage actually consumed (§4).

### 4. Failure context (the part that closes the loop)

When a request or a mapping fails, the log line that reports the failure carries the capture id:

```
User dynamic VM stage=feed-polymer-failed request=http-000412 ep=polymer/feed-all drift=rich_text_nodes[].emoji:object→string
```

Plus a small ring buffer (last 8 capture ids per stage) so a failure without an id still names its input. The
mapper's exception is logged with the drift pointers it touched, not just the stack.

### 5. Tester workflow (one click, one file, one sentence)

1. Reproduce the problem, press **Mark problem** with a short note (the note is what the LLM slices around).
2. Press **Export diagnostic bundle** → `<yyyyMMdd-HHmm>_<appver>.zip` containing:
   `manifest.json` (app/build version, session ids, marker timestamps + notes, capture counts, toggle states),
   `log/yyyyMMdd.log` (the text log, as today), `api/yyyyMMdd.ndjson` (shapes + drift + optional redacted samples),
   `api/baseline.json`, `settings.json` (non-secret toggles).
3. Send the zip. Nothing else is required from the tester.

`document/DIAGNOSTICS.md` grows by the marker-note format only: what you were doing, what you expected, what you
saw. That is the smallest thing a human reliably writes, and it is enough to bracket the bundle.

### 6. Turning a capture into a permanent regression test

`tools/import-capture.ps1 -Bundle <zip> -Marker "<note>" -Name g3/feed-all-emoji-string` writes
`plan/tests/g3/fixtures/<name>.json` (redacted sample, or a shape-only skeleton when sampling was off) and prints
the probe edit to consume it. Probes already build real product source; a fixture from a real device response makes
the next mapper bug fail in CI-less verification before the maintainer ever runs the app.

This is the workflow's real payoff: **device evidence becomes repo artifact**, and "we can't reproduce it here"
stops being an excuse for shipping a mapping guess.

## Phases

| Phase | Work | Done when |
| --- | --- | --- |
| P1 | `Services/ApiCapture.cs` (NDJSON writer, shape builder, baseline store, redaction, caps) + one hook in `HttpHelper` + `SettingHelper` toggle | A device log contains a shape line per response and a drift line for a deliberately malformed endpoint; text log format unchanged |
| P2 | carry `request=` ids into VM failure stages; drift pointers in mapper exception logs; gRPC descriptor shapes | A mapper bug of the 2026-10-31 class names the field and the token type in one line |
| P3 | zip bundle + `manifest.json` + Settings button + `document/DIAGNOSTICS.md` | The maintainer exports one file and it contains marker, log, shapes, toggles |
| P4 | `tools/import-capture.ps1` + fixture loading in g3/g4 | The 2026-10-31 crash is reproduced from an exported bundle by a probe |

## Non-goals

- No request/response logging of secrets, cookies, or signed stream URLs — the redaction rule is a hard boundary.
- No remote upload, no telemetry endpoint, no background reporting.
- No bodyspam: shapes by default, samples on request, bounded totals.
- Not a substitute for the device: the bundle narrows a hypothesis to a fact; it does not run the app.

## P1 状态（2026-10-31）

已落地：

- `Services/ApiShape.cs` —— 路径 → 类型名（`$.data.items[].emoji:object`），深度 8、每对象 64 键、
  数组按元素类型各取一个样本；`Diff` 报新增/消失/类型变化；`Render` 给一行紧凑形状。
- `Services/ApiCapture.cs` —— 每次响应一行 NDJSON（`ts/ep/method/request/status/code/ms/bytes`，
  首次见到写形状并落基线，之后只记漂移那一行），落在 `LocalState/log/api/yyyyMMdd.ndjson`
  与 `baseline-<ep>.json`；单日上限 512 KB；写盘走一个信号量串行，失败一律吞掉。
- `Services/HttpLogSanitizer.RedactBody` —— 名字像凭据的字段替换成 `<redacted>`，再按字节预算截断。
- `Helpers/HttpHelper.cs` —— GET/POST 的成功与 HTTP 失败返回都调用 `ApiCapture.Record`，
  正文为空也记（状态码本身是证据）。
- `Pages/SettingPage.xaml(.cs)` —— 两个开关：记录形状、记录脱敏样本（默认都关）。
- 探针 `plan/tests/cap`（29 条）：形状、上限、数组、漂移、渲染、脱敏、截断，以及接线契约。

仍未做（原计划的 P2/P3）：一次点击导出 zip + `manifest.json`，以及 `tools/import-capture.ps1`
把导出包变成 `plan/tests/g3/fixtures/<name>.json` 回放夹具。
