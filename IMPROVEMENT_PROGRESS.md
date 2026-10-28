# Improvement series progress

## Setup: integration baseline

- The integration baseline and preserved plan are committed in `3ede2d158d954745bf59674efc1e7992c10f9f28`.
- `work/improvements/BASELINE.md` records the target and pinned sibling boundaries, relevant package versions, and explicit project compile inclusion constraints.

## Current step: LM-05 reviewed and published; API-01 next

- Started from clean target HEAD `a8435c1` after the LM-03 reviewed handoff. LM-02 and LM-03 remain reviewed and published. LM-04 setup checkpoint `478e14d` records the batch start. LM-04 implementation and review follow-up are committed and published; see the latest checkpoint below.
- LM-03 added `LocalMediaPlaybackAdapter` to resolve manifest tracks through the saved FutureAccessList folder grant and map every item into the existing DASH player structures. Collection item selection opens `LocalPlayerPage` at the chosen playlist index. `LocalPlayInfo` keeps legacy fields and adds typed video/audio/subtitle/danmaku metadata.
- Manifest playback uses stable encoded collection/item resume IDs. Its history, subtitle, danmaku-network, and danmaku-send paths are guarded; downloaded-video and single-file flows retain their existing routing. FFmpeg local playback uses the resolved `StorageFile` handles. A native-engine failure switches using the manifest tracks, local storage mode, and current playback position.
- `work/improvements/tests/run.sh` passes actual-source LM-01, LM-02, and LM-03 checks. LM-03 covers playlist mapping, default video/audio handles, retained typed metadata, no implicit subtitle selection, stable and non-colliding resume IDs, and stage/collection/item exceptions with the underlying storage exception.
- The actual-source issue #105 disposal and #598 natural-end probes pass for existing report behavior and confirm manifest playback skips Bilibili history. `git diff --check` passed. No app build, restore, launch, protocol generation, or live API call was run. These isolated checks do not prove real UWP storage permissions, native/FFmpeg codec support, or Windows player behavior.
- Added separate manifest video and audio selectors in the existing player pane. The audio selector includes an embedded-audio choice; external tracks reuse the resolved `StorageFile` handles. The existing FFmpeg path handles external audio, while a configured native engine can still open a single video with embedded audio.
- Track switches snapshot the existing local item identity, exact position (including zero), and playing state. The focused coordinator opens the requested pair and on an immediate open failure restores the model/UI and reopens the previous pair once. Legacy single-file and downloaded playback retain their existing unconditional play behavior. Manifest switches do not call the Bilibili URL path or `SetPlayItem`/`SetQuality`.
- `work/improvements/tests/run.sh` passes the LM-04 actual-source coordinator probe plus LM-01/02/03 probes. The LM-04 probe exercises independent video/audio selection, paused position zero, playing position 23.75, and immediate failure followed by one rollback reopen. PlayerControl's item/resume identity gates and MediaOpened seek/autoplay wiring were source-reviewed; the probe does not prove real UWP UI/event timing, codec support, or asynchronous `MediaFailed` recovery. No app build, restore, launch, protocol generation, or live API call was run. `git diff --check` passed.
- LM-05 adds an actual-source subtitle adapter for Bilibili JSON, SRT, and VTT and a loader that parses each enabled XML danmaku file through the existing `NSDanmaku` parser, merges comments in timestamp order, and keeps independent track toggles. Manifest subtitle selection honors only an explicit default; otherwise subtitles start off. Legacy subtitle downloads and user-imported files continue through their existing routes.
- The playlist adapter resolves subtitle and danmaku `StorageFile` handles through the saved FutureAccessList folder grant. The player uses the existing cue model/timer and `DanmuControl`; it clears old visuals/pools on selection changes and uses generation/owner guards so stale reads, cue renders, XML parses, or queued renderer callbacks cannot overwrite a newer selection/item or a disposed player. Per-track failures retain stage, collection/item/track IDs, and full exception details in logs.
- `work/improvements/tests/run.sh` passes actual-source LM-01–05 probes. LM-05 covers JSON/SRT/VTT cues and invalid inputs, VTT metadata/markup, timer cue selection boundaries, asynchronous selection/item/dispose guards, independent danmaku toggle subsets, empty selection, merged timestamp order, and full track failure context. LM-03 now also verifies saved-grant subtitle/danmaku file handles. `git diff --check` passes. These are offline source probes; they do not validate UWP media event timing or actual decoder/renderer behavior. No app build, restore, launch, protocol generation, or live API call was run.

## Historical LM-04 setup checkpoint

- Baseline is clean HEAD `a8435c1`; the LM-03 implementation and handoff are committed. LM-04 will add selectors inside the existing `PlayerControl`, reuse the manifest track handles, and preserve the current item, position, and playing state across reopen. Focused actual-source probes will exercise switch state and rollback. No app build, restore, launch, protocol generation, or live API test is in scope.

## Next step: API-01 compatibility layer and API organization

## Prior step: LM-02 reviewed and published; LM-03 next

- LM-01 remains reviewed and published at the prior implementation commit. LM-02 starts from clean target HEAD `863cf0689796e10f6b8e329bac84316d9336c212`.
- UI entrypoint: add a local media library pivot beside the existing offline downloads in `DownloadPage`; preserve the page's current single-file picker and downloaded-video playback handlers.
- LM-02 service boundary: import a picked `StorageFolder`, load `bili-media.json`, resolve manifest-relative assets through UWP storage APIs, validate with the LM-01 validator, then save collection metadata and a FutureAccessList token. Saved collection rows reopen by token and are revalidated. The page exposes collection details only; item playback remains LM-03.
- Import persistence happens only after every reference validates. Reimport replaces the saved registration for the same root/collection without leaving duplicate access-list tokens.
- Keep failure messages useful and stage-specific, and retain exception details in logs alongside stage and collection ID. Do not log credentials or manifest contents.
- Implemented `LocalMediaLibraryService` with folder import, storage API asset resolution, FutureAccessList token persistence, registry persistence, revalidation on reopen, and reimport deduplication. Failed imports leave prior registrations intact; failed reopen keeps the saved row available for repair/reimport.
- Added a local media library pivot to `DownloadPage` for importing folders, listing saved collections, and viewing validated item titles. The existing single-file picker and offline download player handlers remain unchanged. Collection items do not start playback until LM-03 supplies the adapter.
- `work/improvements/tests/run.sh` passes actual-source LM-01 checks and LM-02 service checks with stubs for UWP storage, persistence, and FutureAccessList boundaries. The harness covers import/reopen, invalid paths, missing assets, retained saved entries after failed reopen, and reimport deduplication. It does not prove real UWP permissions or process-restart behavior.
- No SRT/VTT parser or runtime playback support was added; formats remain manifest declarations only until LM-05.

## Historical LM-02 handoff: LM-03 playback adapter

- Map manifest items into the existing `PlayInfo` / `BiliPlayUrlInfo` path while preserving the legacy single-file and offline-download playback flows.
- Do not add playback controls to the collection UI until those items are connected to the player.

## Prior completed step: LM-01 reviewed and published

- Implemented typed schema-v1 collection, item, and track models; a JSON loader; and a validator in `Models/LocalMedia` and `Services/LocalMedia`.
- The loader raises contextual `LocalMediaManifestException` errors. LM-02 must catch and display these errors in the app.
- Validation checks required fields, version, duplicate item/track IDs, declared defaults, supported extensions/formats, root-contained relative paths, and referenced file existence through a callback. It leaves absent defaults unset and accepts missing labels.
- Format declarations and extensions only indicate manifest support. They do not claim decoder support. SRT/VTT parsing and rendering remain unsupported until LM-05 adapters and tests exist.
- Isolated actual-source checks passed with `work/improvements/tests/run.sh`: 12 validation cases plus filesystem loading. No UWP app build, restore, or launch was run. `git diff --check` passed.
- Next batch: LM-02 folder import and storage access, preserving the existing single-file and legacy download playback flows.

## Remaining batches

- API-01: introduce the API compatibility layer and service organization. The pinned sibling `/workspace/biliuwp-lite` was absent during LM-05; source-dependent behavior ports should wait until that reference is available through the authorized workspace.
- API/auth, gRPC, danmaku, and notification ports as scoped by `work/improvements/PLAN.md`.
- SponsorBlock is excluded entirely. Live tiles remain optional; the player toast service is the planned first notification slice.

## Review and resume checkpoint

- LM-01 implementation: `87b3480d5fe2812fcc6071bc17fb9a5238883054`; root reviewed the source, project entries, and actual-source test scope. Normal push to `origin/work` succeeded through this commit; this handoff is published separately.
- Root whitespace check: `git diff 3a4fe55 --check` passed. The agent ran the isolated unit harness; no app build, restore, runtime or live API test was performed. No pull/fetch was performed.
- Resume with LM-03: map validated manifest items into the existing player while preserving downloaded-video and single-file flows. Do not claim folder playback until the adapter is integrated; preserve explicit subtitle defaults and avoid advertising SRT/VTT playback before LM-05.
- Keep the existing player engine, old single-file/download paths and NSDanmaku backend. Use the pinned sibling for ports and the preserved plan for the new manifest contract.
- Prior bug series remains 14/15 fully fixed: fully unmarked AI subtitles (#1311) and old labels remain unresolved.

## LM-02 publication checkpoint

- Setup checkpoint: `c877ab5`; collection import implementation: `e939a6b`; exception-detail logging correction: `8be76ef`. Root reviewed the service, UWP caller boundaries, explicit project entry, and isolated test scope.
- Normal push to `origin/work` succeeded through `8be76ef`; this handoff is published separately. No pull/fetch was performed. Root `git diff 863cf06 --check` passed.
- The agent ran the actual-source LM-01/LM-02 harness with storage/FutureAccessList stubs. Real Windows picker, persisted permission across process restart, and UI remain for user app testing. No app build, restore, launch, protocol generation or live API test was performed.
- Resume at LM-03: use saved folder tokens to resolve `StorageFile` assets, adapt collection items to existing playlist/player models, and wire item playback. Preserve legacy single-file/download behavior and explicit subtitle defaults. Current collection controls import/reopen/view only; no collection playback or SRT/VTT rendering is claimed.

## LM-03 publication checkpoint

- Setup checkpoint `e28d9d4`; implementation `1bc25b6`. Root reviewed the playlist adapter, model additions, caller/index routing, engine file-handle handling, manifest-specific offline branches, and focused probe scope. Normal push to `origin/work` succeeded through `1bc25b6`; this handoff is published separately.
- Root `git diff d89722f --check` passed. The agent ran the LM-01/02/03 actual-source harness and extracted #105 Dispose / #598 natural-end probes. Manifest history suppression is covered by those probes; decoder behavior, engine-switch event timing, and UI/storage permissions are source-reviewed only and require user app testing. No app build/restore/launch or live API test was performed. No pull/fetch was performed.
- Resume at LM-04: add video/audio selectors using typed local track handles, preserve position and playing/paused state during switches, and retain native single-file and FFmpeg external-audio routing. LM-05 still owns subtitle parsers and manifest danmaku loading; these tracks are currently metadata only.

## LM-04 publication checkpoint

- Setup `478e14d`, implementation `90ac3de`, item identity follow-up `b543150`. Root reviewed selector wiring, actual switch coordinator and tests, local playback routing/state handling, and failure logs. Normal push to `origin/work` succeeded through `b543150`; this handoff is published separately. No pull/fetch was performed.
- Agent LM-01–04 isolated probes passed; root `git diff a8435c1 --check` passed. Coordinator tests cover position/play-state callbacks and immediate failure rollback, not UWP media events or real decoding. No app build, restore, launch, protocol generation, or live API test was performed.
- LM-05 completed in the following checkpoint.


## LM-05 publication checkpoint

- Implementation `8106491` reviewed by root and pushed normally to `origin/work`. Root reviewed saved-grant file handles, parser conversion into existing cue models, subtitle defaults/off selection, independent enabled XML track merging, project/XAML entries, and async load/render boundaries.
- Root ran the LM-01–05 offline runner successfully, including 22 LM-05 assertions. Tests exercise actual parser/merge/guard helpers with storage and player model stubs; UWP UI wiring and the NSDanmaku parser/render boundary were source-reviewed, not executed. Project/XAML XML checks and `git diff 01be302 HEAD --check` passed. No application build, restore, launch, protocol generation, or live API tests were run.
- The restored checkout initially contained only the master baseline. Before the handover was available, root fetched the user-identified remote work branch and fast-forwarded the clean local work branch to `01be302`; no further fetch/pull occurred. The missing standalone test SDK was downloaded to the documented offline tools path.
- Resume API-01 per the preserved plan. The pinned sibling is absent in this restored environment; check its pinned revision before source-dependent behavior ports. Real UWP permission persistence, UI/media timing, decoding, and renderer behavior remain for user app testing. SRT/VTT styling/layout metadata is reduced to plain cue text in the existing subtitle renderer.
