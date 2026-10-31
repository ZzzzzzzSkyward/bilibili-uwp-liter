# Plan — open work only

Updated 2026-10-31 for `work`. Everything shipped is one line in the commit history; the offline
probe suite lives in `plan/tests/` and is green. Finished plan documents, reports and progress logs
were removed on purpose — `git log` has them.

**Verification policy:** the maintainer's device pass counts as passed. A shipped item stays closed
until the maintainer reports an actual bug; only such a report reopens it, and then it is written up
in `bug_report.md` first.

## State

| Fact | Value |
| --- | --- |
| branch | `work`, in sync with `origin/work` |
| probes | `plan/tests/<name>/run.sh` — c1 52 · cap 38 · c2 99 · c3 97 · d1 82 · d2 58 · g2 22 · g3 73 · g4 131 · g5 44 · ov 29 · stab 53 · smart1 160, all green |
| build | `powershell -NoProfile -ExecutionPolicy Bypass -File build-release-x64.ps1 -Fast -NoVersionBump` (compile only) or without `-Fast` to package into `Z:\` |
| sync | `powershell -NoProfile -ExecutionPolicy Bypass -File sync-work.ps1` — fetch+rebase+push with TLS retries, stashes the two `Package.appxmanifest` around it (`-NoPull`, `-NoPush`, `-Attempts`, `-RetrySeconds`) |
| dependency plan | `plan/DEPENDENCY_UPGRADE.md` (batch 1 done, batch 2 = protobuf toolchain, batch 3 = migrations) |
| review cache | `plan/REVIEW_LOG.md` |
| bug device log | `bug_report.md`, `code_review.md` at the repo root |

## Done

Newest first; every one of these is device-accepted per the policy above.

- **N1 — player overlay toasts:** `312fa23` — `Controls/PlayerToast.xaml(.cs)` and
  `Services/PlayerToastService.cs`, keyed overlays (volume, brightness, seek, message, acceleration,
  speed) replacing the central `ToolTip` label, one service per `PlayerControl`, timers stopped in
  `Dispose`; build 0 errors.
- **Release tooling:** `build-release-x64.ps1` now captures the whole `-> … .msix` path (spaces
  included), derives the two expected package paths from `OutputDir` + version, refuses to continue
  when neither `BiliLite.UWP_*` nor `BiliLite.Packages_*` is found, and names the `CS2012` holders
  through Restart Manager before retrying. Verified with a full packaging run into `H:\uwp\pkg diag`.
- **BR-16 — quiet load-more:** `e5c97e1`, `261317f` — silent page-2 failure, legacy text in the only
  toast, gRPC cursor preserved so the retry resumes.
- **API-SMART-01:** `797dc3a`, `26d4e57`, `3665bdb` — adaptive gRPC/legacy selection for six dynamic
  paths, persisted `dynamic-api-history.v1.json`.
- **C1/C2/C3:** `92ecd6a`, `34ec312`, `c735a73` (+`18f71dc`) — collections, playlist paging, playlist
  links and continuation.
- **D1/D2:** `5e36ec2`, `e94c843` — danmaku list with copy/like, filter import/export.
- **G1–G5:** `cfcd3de`/`75e8ecb`, `914d32f`, `d4c6345`(+`6ae1981`), `65d5e8c`(+`4fca416`),
  `b39739a`(+`9fb3d16`).
- **Stability 1/3/4:** `96de3fc`, `0668875`, `546b409`.
- **Home-feed REST fallback (BR-13):** `5881dbb`.

## Open

| # | Item | State |
| --- | --- | --- |
| 1 | Player open/replace/close session ownership | not started |
| 2 | Background playback ownership (SMTC + memory budget) | not started, needs device evidence (BR-21 recorded 2026-10-31) |
| 3 | Response-evidence workflow (shapes, drift, bundle, fixture import) | done — capture (shapes/drift, redaction, two toggles), one-click zip bundle with `manifest.json`, and `tools/import-capture.ps1` turning a bundle into probe fixtures; only real-bundle fixture drops remain |
| 4 | API parsing structure: names, fields, and parsing methods | P0 inventory, P1 contract, P2 web half and the P3 legacy half are done — `ApiEnvelope` is the single REST envelope reader, `LegacyDynamicFeedParser` owns the four old-endpoint legs, gRPC mapping stays protobuf-only; P4 other features next |

## 1. Player session ownership

`PlayerControl` opens media in several paths (native DASH, FFmpeg DASH, quality change, engine
fallback) and mutates shared player fields across `await`s. Closing the tab or switching video while
an open is in flight lets the old operation resume against a replaced/cleared player, attach A's
source to B's player, or leak resources created after the close routine finished. `SetQuality` starts
`ChangeQuality` without awaiting it; `Player_ChangeEngine` starts another open on media failure with
no in-flight guard.

Fix shape: one session id per open, new resources kept local until the session is accepted, pending
opens invalidated on close/replacement, resources of an obsolete session disposed, callbacks from
obsolete players rejected. Do not rewrite the player.

## 2. Background playback ownership

`backgroundMediaPlayback` is declared in both manifests and `PlayerControl` wires Play/Pause and
status, so missing permissions are not the issue. The gap is ownership: system media controls are
view-scoped, and `OnUnload` disables them for every control, so a departing tab can disable the
controls of the player that is still playing. `App.OnSuspending` also does no player work and forces a
finalizer wait inside the suspension window.

Fix shape: one active playback owner keeps the SMTC session across tab and visibility changes, optional
visual/danmaku work is released when backgrounded, and suspension work stays bounded. Do not recreate
the player on every foreground return.

This one has no device evidence behind it — only source reading. It needs the maintainer's
observation (minimize vs tab switch, engine, crash details) before it is worth a refactor.

## 4. API parsing structure — names, fields, and methods

**Goal:** give each API response a clear route from transport to an application model. A caller should
know which endpoint produced a field, what that field means, and which method parses it. Start with
the dynamic feeds implicated in BR-17; migrate working features incrementally.

### Responsibilities and locations

The first implementation batch should inventory the existing paths before moving anything:

- `Api/` holds request builders. The general web dynamic feed now lives in
  `Api/User/WebDynamicFeedApi.CreateFeedRequest`, so the old `ArticleV2` name is gone.
- `HttpResults.GetJson/GetJObject/GetData/GetResult` in `Helpers/HttpHelper.cs` still decode responses
  and sometimes show toasts; `ApiDataModel<T>` and `ApiResultModel<T>` describe two different
  response envelopes, and neither is the parse result.
- `Helpers/DynamicGrpcMapper.cs` is protobuf-only again: the web half moved to
  `Parsing/Dynamic/WebDynamicFeedMapper.cs` with the shared scalar readers in `WireScalars.cs`.
  The two feed VMs decode through `Parsing/Dynamic/WebDynamicFeedParser.cs`; legacy REST responses are
  still deserialized inline, which is what P3 separates.

Use these boundaries for migrated features; the paths are proposed destinations, not files added
by this planning change:

| Boundary | Responsibility | Proposed location |
| --- | --- | --- |
| Request | Endpoint, parameters, authentication/signing, request construction | `Api/<Feature>/` |
| Transport | HTTP status/body or gRPC bytes/status; no interpretation of feature fields | Existing `HttpHelper` / `GrpcRequest` |
| Wire DTO | One source's envelope and payload fields; explicit JSON names | `Models/Api/<Feature>/<Source>/` |
| Parser | Decode and check the envelope/field shapes; return data or a parse failure | `Parsing/<Feature>/` |
| Mapper | Convert a source DTO or generated protobuf reply into the existing application model | `Parsing/<Feature>/` |
| Service / VM | Choose sources, own paging and request generations, publish rows and user messages | Existing services and VMs |

Keep the current HTTP library, protobuf generator and renderer. Do not introduce a second transport,
a dependency-injection framework, or a repository-wide rename to achieve these boundaries.

### Naming contract

| Kind | Convention | Dynamic-feed example |
| --- | --- | --- |
| Endpoint/request builder | `<Feature>Api`, with source named when variants coexist; builders are synchronous | `WebDynamicFeedApi.CreateFeedRequest(...)` |
| Network operation | `Get...Async` / `Load...Async`; `Async` means actual asynchronous work | `GetFeedPageAsync(...)` |
| Source DTO | `<Source><Feature><Payload>Dto`; C# properties use PascalCase and explicit wire-name attributes | `WebDynamicFeedItemDto`, `[JsonProperty("id_str")] DynamicId` |
| JSON parsing | `Parse<Source><Payload>` returning `ApiParseResult<T>`; no network or UI work | `ParseWebDynamicFeed(...)` |
| Conditional scalar read | `TryRead<Type>`; failure is distinguishable from a legitimate zero/false | `TryReadInt64`, `TryReadBoolean` |
| Source mapping | `Map<Source><Payload>`; share domain helpers only after source differences are handled | `MapWebDynamicItem`, `MapGrpcDynamicItem` |
| Normalized page | `FeedPage<TItem>` with `Items`, `NextCursor`, `HasMore` and source identity | Existing card models remain the items |

`ApiParseResult<T>` is a proposed small result type containing the parsed value or a failure category
and context. Do not treat `ApiDataModel<T>`, `ApiResultModel<T>`, `ReturnModel<T>` and
`DynamicApiSourceResult` as interchangeable: wire envelopes, parsing results and source-selection
results serve different purposes. Keep compatibility wrappers while callers migrate. Replace names
such as `ArticleV2`, misspelled builders and source-ambiguous mapper methods when their feature is
migrated; do not rename unrelated APIs in the same batch.

### Field contract

Maintain a compact field table beside each migrated parser: logical endpoint/source, wire path,
wire type(s) actually observed, normalized property/type, required/optional status, missing/null
behaviour, and conversion rule. Include the caller or fixture that establishes the rule.

| Field family | Rule |
| --- | --- |
| Envelope | Explicitly select `data` or `result` for the endpoint. A missing or malformed `code` is not implicit success. Normalize `message`/`msg` at this boundary. |
| IDs | Use descriptive names (`DynamicId`, `Aid`, `Cid`, `UserId`) and keep opaque IDs/cursors as strings. Convert to numeric IDs only where the endpoint contract requires it. |
| Nested values | Check object/array shape at every boundary. Missing, JSON null and the wrong token type are distinct; `?.` alone does not make a `JValue` indexable. |
| Numbers and flags | Centralize culture-independent readers. Accept numeric strings or `0`/`1` only where the endpoint's observed contract permits them; never turn a failed required parse into zero/false. |
| Counts and time | Separate display text such as `1.2万` from a numeric count. State timestamp units and distinguish epoch times, durations and presentation labels. |
| Optional content | Apply a documented default or preserve text when optional metadata is unusable, such as a string-valued `emoji`. Unknown card types should retain an existing unsupported-card placeholder. |
| Paging | Keep web `offset`, legacy dynamic IDs and gRPC `historyOffset/updateBaseline` separate and source-bound. A failed append keeps the last successful cursor. Server `has_more`, cursor advancement and filtered rows are separate facts. |

Use DTO deserialization for stable payloads and typed `JToken` readers for genuinely polymorphic
fields. Neither method should require serializing a token back to text merely to parse it again.
Add a shared `ReadObject`/`ReadArray`/scalar helper only when it replaces duplicated rules; keep
endpoint-specific aliases, units and defaults inside that endpoint's parser.

### Parsing and failure contract

1. Transport reports HTTP/gRPC status independently of the response body.
2. Decode once, check the endpoint's envelope, then parse its payload. Separate transport failure,
   server/business rejection and malformed payload; cancellation or a superseded request is not a
   parser failure. A valid empty page is not malformed.
3. Map the source payload into the existing application model. Missing optional metadata may
   degrade one card; a broken required envelope makes that source attempt fail. Record the policy
   for a malformed item instead of silently dropping an entire page.
4. Return the result without toasts or UI mutations. The source-selection service decides whether
   to fall back; the VM alone decides what to publish and which user message to show. Publish rows
   and new paging state only after the page is accepted and its request generation is still current.
5. Attach a logical endpoint/source, stage, field path, expected/actual token type and available
   request ID to parse failures. Reuse the response-evidence workflow in item 3 for captured shapes
   and fixtures rather than creating another diagnostics system.

### Implementation batches and completion criteria

| Batch | Work | Complete when |
| --- | --- | --- |
| P0 — inventory **(done, `plan/API_PARSING_INVENTORY.md`)** | List the dynamic all/video/article, detail, space and repost endpoints, request builders, envelopes, field tables, parsers, mappers and callers. Identify shared rules versus source-specific rules. | Each active response path has a named owner; no existing source or paging cursor is omitted. |
| P1 — minimal contract **(done)** | Define `ApiParseResult<T>` and only the field readers needed by the first feature. Preserve existing callers through adapters. | Success, business rejection, malformed envelope and valid empty payload have distinct, documented results; parsers perform no UI work. |
| P2 — web dynamics **(web half done)** | Extract the web JSON part of `DynamicGrpcMapper` into the web parser/mapper, rename the general feed builder through a compatibility wrapper, and remove direct web-field parsing from the two feed VMs. | All/video/article retain their current rendering and source-bound cursors; the recorded string-emoji failure is handled; malformed pages reach source fallback without partial publication. |
| P3 — other dynamic sources | Separate gRPC mapping from legacy REST parsing, reusing the existing application card model. Migrate detail, space and repost callers one path at a time. | Source differences remain explicit; comments, links, empty pages and failed-append retry semantics are preserved. |
| P4 — other features | Apply the same structure to comments, video/season detail, playlists and account responses in separate batches, driven by actual callers. | Each migrated feature has one documented parsing entry point and field contract; untouched features continue through their existing adapters. |
| P5 — retire duplicates | Remove old parsing wrappers/model aliases only after their callers are gone; update the inventory and field tables. | Source inspection shows no parallel parser for the same response contract or transport/UI work hidden inside a parser. |

Use existing `plan/tests/g3`, `g4`, `g5`, `smart1` fixtures and captured device evidence as the
behaviour reference for implementation review. Acceptance includes large IDs, optional/mistyped
nested fields, unknown cards, empty pages, separate source cursors, failed append/retry and refresh
replacement. This is a plan only: no application code, fixtures, tests, builds or execution are
part of this addition, and it makes no new validation claim.

## Parked

- **Live tiles:** optional; needs the tile service plus a background-task registration whose async
  refresh is awaited before the deferral completes.
- **Real buvid acquisition:** no source-backed flow; app-key headers do not promise it.
- **Alternate danmaku renderer:** nothing needs FrostMaster; keep NSDanmaku.
- **More API/auth variants:** no caller; do not port endpoint classes to mirror the sibling.
- **Local-library expansion:** the five milestones are done; more discovery/formats/artwork needs its
  own request. Keep the current manifest models as the contract.
