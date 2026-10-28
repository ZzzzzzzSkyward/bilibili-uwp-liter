# Improvement series progress

## Setup: integration baseline

- The integration baseline and preserved plan are committed in `3ede2d158d954745bf59674efc1e7992c10f9f28`.
- `work/improvements/BASELINE.md` records the target and pinned sibling boundaries, relevant package versions, and explicit project compile inclusion constraints.

## Current batch: LM-01 manifest foundation

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
