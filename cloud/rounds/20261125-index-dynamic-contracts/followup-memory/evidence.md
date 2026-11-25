# Repeated-close audit evidence — FROZEN / RELEASED

**Disposition: bounded no-fix; native attribution conditional.** All new execution was standard-library read-only binding verification. No new behavioral fixture, shared suite or native run was performed. The current batch is not recertified by this discovery.

## Exact inputs and commands

Private root: `/workspace/work/index-dynamic-contracts/followup-memory-discovery/`. Complete source bytes came from `git --no-optional-locks ls-tree -r --name-only 7fc3c524fde4139d01e7ecd362647ca771c64ea8 src/BiliLite.UWP` and `git --no-optional-locks show 7fc3c524fde4139d01e7ecd362647ca771c64ea8:<path>`. These read objects only; no checkout/fetch/ref/index action occurred. Snapshot86 includes player/page/shell/frame, Services/Helpers, detail/player VMs and queue/sequence classes. Capturing complete files does not mean every unrelated method received a new behavioral test.

Final command: `python3 /workspace/work/index-dynamic-contracts/followup-memory-discovery/verify-bindings.py`, **exit0**, zero mismatches. It checks86 A hashes,86 private copies,86 working relevant files;47 accepted player inputs;12 accepted season sources plus7 historical season fixtures;6 unchanged historical root-model inputs;7 prior raw artifacts and4 exact .NET tool files. New behavior tests0/native runs0. Python/Git executable/version/hash and actual commands are in the private report. Host is Linux; no Windows UWP/VS/WPR/WPA executable was found on PATH. Portable tooling does not provide Windows runtime observation.

| Private artifact | SHA256 |
| --- | --- |
| verify-bindings.py | `901e5c7596bcc489164e45684f276a1a51d1b6403679bf525ddd6da0bcaa0f02` |
| binding-verification.json | `0d79bad38d6af3006549249aa44e7d8e5f60e165d3098a92f17cbab345382b91` |
| source-manifest.json | `7ffe20a7cc08d7ef786fa04b9bba3e83052a5a4303fca0d9870afcd8ceaa0b18` |
| player-reuse-inputs.json | `7fdfeddf2dd3def87a0593febddbf186db19ba3ada9c7a7b5eed34079918f5b9` |
| season-reuse-inputs.json | `a1378c10a1477994314281d593c90a633f251a38a918307c75e265aac09433c6` |

Full relevant source hashes are in source-manifest.json. Principal identities: PlayerControl `262e0719cf4589ebd9e493937f377bcc95d43e092962aad30dbfc137275bcb89`; Player `97394caf6013c2588f7137ae9d267b24f7247189e418ba897336cb8ae89ac4b0`; Season page `c91ceea2e58aa8318311841eaf22ef473711710c6222289baa6bed9b79bf4f5d`; Season VM `615a4c04bb0f4077ff63976e993f013a455b6fcf55a77fc798cef0a9d5acc596`.

## Actual caller/root assessment

All references below are A source under `src/BiliLite.UWP/`.

| Root/caller | Close boundary and bounded assessment |
| --- | --- |
| MessageCenter static shell events → MainPage/NoTabMainPage → tab/MyFrame/page | New tab creates a new frame. MainPage.ClosePage191+ invokes frame.Close, nulls tab content and removes the tab. MyFrame.ClosedPage is frame-owned. NewInstanceFrame.GoBack248+ disables outgoing cache, clears ForwardStack or closes/removes the extra frame after its animation. Forward-retained pages are intentionally live. Actual Frame cache delivery remains native. |
| View DataTransferManager → Video/Season | Actual Close/OnNavigatedFrom remove DataRequested; frame ClosedPage subscriptions use remove/add and explicit release. Video key subscription and player view subscriptions additionally rely on actual Unloaded. Local frame/page cycles alone provide no independent global root. |
| Video.ClosePage153+ → transition/queue/sequence | Close invalidates transition, RemoveMediaListPlaylist drops loader/queue state, sequence.Clear replaces its scalar position. Complete PlayQueueState.Clear cancels LoadAll and nulls four owner references. MediaListPageLoader is instance/single-flight and clears m_running in finally; a held fetch delegate is pending work, not evidence of an immortal registry. |
| PlayerControl Loaded482+/Unloaded466+; Dispose4247+ | One static _activeInstance slot, idempotent view event registration/removal. Terminal close invalidates session/load generations, detaches interaction children, stops subtitle/history/danmaku work and tears Player down. Focus/view/SMTC cleanup expects Unloaded. Existing native teardown-throw/display/SMTC conditions are unchanged; ordinary success is not proof of native release timing. |
| Player/session/toast/danmaku | Player.ClosePlay1713+ clears owned native fields/lists under nonthrowing boundaries. PlayerSessionState has one current ticket and an instance held list cleared on transfer/invalidation. PlayerToastService reuses current caller keys and Dispose stops/detaches timers, clears entries and removes its stack. No new static per-close owner list was found. Active danmaku pools and finite task/dispatcher snapshots remain owner/work-bound. |
| TapToolTip/history/diagnostics/caches | Tooltip tables use ConditionalWeakTable, not strong key dictionaries; its timer stops on close/Unloaded or four-second tick. AB history stores scalar intervals by content and ordinary open calls Find, not append. AB edits/resume persistence remain intentional; the startup200 cleanup is not misrepresented as a strict insertion cap. WBI stores current key/in-flight work, not per-video owners. LogHelper config targets files; ApiCapture is default-off, serial and releases its gate in finally. Stalled enabled capture can retain pending responses; no permanent close leak is inferred. Download mutation registries are outside the ordinary playback caller chain. |

## Reused proof, without rerun

[Managed source evidence](../../20261125-video-memory/managed/evidence.md), [accepted M1 review](../../20261125-video-memory/managed/final-review.md) and [final IG1 fresh audit](../../20261125-video-memory/ig1-final-audit.md) retain their limits. Exact raw artifacts were hash-checked, not executed again:

- Historical `managed/proof.log` SHA `410221e3…`, command `dotnet /workspace/work/video-memory/managed/ManagedMemoryProof.dll`, exit0/30 descriptive assertions. Only unchanged shell/video/local root controls carry: active50 retained/share subscribers50, ordinary close0 subscribers/0 survivors; omitted-close root control retains then releases; local50 collect; no-tab50 open/back returns to one home frame. Six relevant source inputs match A. Its old Season/Player unsafe cases are historical and are **not** accepted current behavior.
- `season-final-review/fixed-proof.log` SHA `c2f0e17d…`, recorded `dotnet .../fixed/plan/tests/season1/bin/Season1Probe.dll`, exit0: original141 plus497 additive checks. Both50 held-close gates have zero late request/notification/title/review/player-attempt/comment/QR/directory effects, with active/live/fresh/terminal/reverse/error/root controls. Its12 sources and7 exact fixture inputs match A; current FI1 additions are outside this historical fixture reuse.
- Final IG1 playersess log SHA `ebbfd754…` preserves66 residual/39 lifetime/five graphics groups and original240 assertions. All47 accepted source/fixture inputs match A. Prior owning/fresh receipts establish terminal/live/fresh, stale work, history and graphics boundaries; this audit adds no Windows or fifty-watched-video result.

Managed fixtures double platform delivery; test-only reachability collection is not application GC or RSS evidence. Complete actual fifty decoded-video/RAM/managed-root/native-heap/GPU measurements remain absent. The existing Windows protocol and the new requirements' conditional acceptance are the precise next steps. No supported defect, implementation request or current archive dependency remains. All custody released.
