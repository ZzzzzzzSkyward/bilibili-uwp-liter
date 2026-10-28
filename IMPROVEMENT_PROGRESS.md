# Improvement series progress

## Setup: integration baseline

- The integration baseline and preserved plan are committed in `3ede2d158d954745bf59674efc1e7992c10f9f28`.
- `work/improvements/BASELINE.md` records the target and pinned sibling boundaries, relevant package versions, and explicit project compile inclusion constraints.

## Current step: LM-03 playback adapter

- Starting from clean target HEAD `d89722f`. LM-02 remains reviewed and published.
- Adapter boundary: resolve validated manifest media through the saved FutureAccessList folder grant, map the default video/audio to the existing local DASH player structures, and navigate selected collection items through `LocalPlayerPage`. Typed manifest track metadata remains available for LM-04/LM-05; legacy local download properties stay intact.
- Guard manifest-backed collection sessions against Bilibili history, subtitle, danmaku, and send calls while preserving downloaded-video and single-file behavior. Use stable collection/item resume keys. External audio uses the current FFmpeg-backed local path where required; no player engine is added.
- Setup checkpoint recorded before implementation. Focused actual-source adapter tests will stub storage and player model boundaries. No app build, restore, launch, protocol generation, or live API calls are in scope.

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

## Next step: LM-03 playback adapter

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

- LM-03: compatibility adapter to the existing player.
- LM-04: video and audio track selection.
- LM-05: subtitle and danmaku track loading, with the documented subtitle parser limits respected.
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
