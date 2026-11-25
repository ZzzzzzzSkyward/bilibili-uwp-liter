# Player lifetime and graphics implementation — FROZEN / RELEASED

Owner: memory_player_executor, gpt-6.1-sol/high. Accepted scope: R1/R2/R3/RR1 plus G1 under the active combined `../execution-dispatch.md`; accepted requirements/review hashes remain unchanged. Local HEAD remains `7356b94893377558191b0d70576a52bae704c1f9`. The explicit dirty continuation after root's clean startup sync applies. No Git fetch/index/staging/commit/push/ref/history/auth/settings action, no main-document edit and no other owner's source edit occurred. Ten scoped code/test files and this evidence are frozen for independent implementation review and fresh async residual review; further changes require resuming this writer. There is no source implementation blocker within the grant.

Original source was captured **before edits** at `/workspace/work/video-memory/player-fix/original/src/BiliLite.UWP/Controls/PlayerControl.xaml.cs`, SHA256 `18fac7cc1afcdf38f5896364f1606dd05446358965f3aae51cff00a7929b1e4f`, exactly matching activation. Final full source SHA256 is `8682d1505752a2dfd99e835259d45429cf78ee3e358caf39553b1375a7d4a3cc`. Full file/test/dependency/tool/extraction bindings and commands are in [implementation-evidence/bindings.json](implementation-evidence/bindings.json); artifact meaning and boundary declarations are in [implementation-evidence/README.md](implementation-evidence/README.md).

## Exact changed/new paths

Changed:

- `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs`.
- `plan/tests/playersess/Program.cs`, `PlayersessProbe.csproj`, `run.sh`.

New under the same existing owning stage:

- `plan/tests/playersess/PlayerLifetimeProbe.cs`, `PlayerLifetimeStubs.cs`.
- `plan/tests/playersess/SubtitleGraphicsProbe.cs`, `SubtitleGraphicsStubs.cs`, `SubtitleGraphicsBaseline.cs`.
- `plan/tests/playersess/extract-player-lifetime.py`.

Existing Program bytes are identical after removing only the four added dispatch lines plus their blank line. All original240 assertions and all existing history checkpoint/storage fixture/stub bytes remain intact. The original session/history/local-media guard/coordinator services, Player.xaml.cs, UWP project/package/dependencies, renderer policy/helpers, manifest/runner/stage inventory, page/VM and other owners' files were not edited. No new aggregate stage: inventory remains51 logical/52 executable.

## Source behavior

Dispose now sets a monotonic terminal latch before teardown callbacks and returns on repeated close. InitializePlayInfo and Loaded reject a terminal owner before display/global binding/timer work. Manual subtitle, direct subtitle, public index, import, local open and engine entry routes also reject it. Unloaded continues its normal view detachment, including after close; it neither disposes a live owner nor resets the latch. New legitimate ownership is constructed through existing fresh page/frame paths. Retained live controls still initialize/replace the same PlayInfo reference and unload/load normally. The readonly toast service, disposed media owner and nulled resources are never reconstructed.

The current-work rule checks terminal state, original item reference and original operation generation. SetPlayItem advances that generation, invalidates existing sessions/track tickets and checks after every child await. SetQuality rejects late success/failure before menu changes or ChangeQuality can mint a session. GetPlayerInfo rejects after metadata, interaction-node and online awaits before control publication. ChangedNode captures generation/item/VM and rejects old completions before danmaku/quality work. A replacement detaches the old interaction VM even for the same PlayInfo reference, preventing a pending old VM from remaining bound. Normal node interaction within a live generation stays usable. Synchronous close/replacement callbacks at ClosePlay/change-episode boundaries also prevent the superseded SetPlayItem from proceeding.

Picker handlers capture work identity before showing the picker, so a returned file cannot recapture a replacement/closed owner as fresh authorization. The existing guarded import/load methods remain complete and are terminal-gated. SetDanmaku's segmented update checks before its second load/toast/finally publication; LoadDanmaku rejects stale results/finally changes. New item generations reset the current load/switch flags so stale completions cannot release a replacement's flag.

Local track switching captures the item generation through coordinator callbacks and its finally block. The existing coordinator restores the same mutable LocalPlayInfo on failed open. An obsolete callback therefore returns an internal successful no-op result, including an obsolete open fault, so the unchanged coordinator cannot roll back the replacement's shared model. The outer current-work check rejects all publication. Current-operation failures still execute the original rollback/diagnostics. Both completed-failure and faulted-open50-cycle same-reference mutable-model controls execute the actual coordinator and repaired callbacks. No exception is masked as a caller/page fix; independent season caller attempts remain separately decisive.

All three settings loaders still execute every refresh assignment on each load. Per-loader flags register their existing anonymous handlers once; refresh guards suppress persistence/toasts from programmatic reapplication. One later user action retains its normal write/render behavior. Subtitle refresh explicitly requests a render update; persisted false hide flags also re-enable the corresponding live danmaku lanes. Ratio/rate/volume/autoplay/AB/settings behavior remains covered by the old assertions and additive settings controls. Repeated Loaded also removes the same key handler before reattaching it. The history timer's constructor Tick registration and synchronous final progress persistence/report behavior are unchanged.

G1 uses ordinary nested using scopes for format→target→layout→geometry at their existing constructors. Format setters execute inside its acquired scope. Stroke style is assigned after Clear, its setter/draw execute inside an enclosing try, and finally releases it after drawing/stream consumers. Completion release order is style→geometry→layout→target→format. Drawing-session close→awaited PNG save→awaited source load→stream close→output assembly stays intact. All parameters, text normalization, output graph ownership and exception propagation remain unchanged. The borrowed shared device and returned Grid/Image/BitmapImage remain open. No dependency or renderer policy change.

## Final actual-source proof

Private driver: `python3 /workspace/work/video-memory/player-fix/run-comparison.py`, exit0. It invokes the same command twice with unchanged final fixture/stub/extractor/dependency/compiler/runtime bytes:

```bash
UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality \
PLAYER_LIFETIME_SOURCE=<original-or-final-absolute-PlayerControl-path> \
PLAYER_HISTORY_SOURCE=<same-original-or-final-path> \
PLAYER_LIFETIME_EVIDENCE_DIR=/workspace/work/video-memory/player-fix/<original-or-fixed>-extraction \
bash plan/tests/playersess/run.sh
```

Original source exits1 through desired-safe assertions, **not a compile failure**. Final source exits0. Exact expanded paths/environments/source hashes and before/after input/tool equality are recorded in bindings.json and private comparison-status.json. Final extraction binds39 complete actual methods; original binds38 because the current-work helper was absent. No synthetic SetPlayItem/restoration implementation or private terminal-flag reset is used. Historical source-wiring assertions run as such and are distinct from behavioral actual-method evidence.

| Acceptance | Original source / same assertions | Final source / same assertions |
| --- | --- | --- |
|50 pending-info close cycles |50 modeled subtitle timers/50 closed survivors; desired-clean FAIL |0 new subtitle loads/timers/survivors |
|50 pending-quality close success/failure |50 new opens/sessions on success;50 stale failure dialogs |0 opens/sessions/stale dialogs |
|Active50 / ordinary-close50 / empty-info50 / pending-subtitle-close50 |Expected50 active roots; closed controls0 |Same positive/negative behavior |
|Public live/fresh same-reference, terminal entries, live unload/reload |Disposed Initialize/Loaded throw and late entries do work |50 cycles each quiet after terminal close; live/fresh open/play/subtitle behavior preserved |
|Distinct/same-reference old quality/info success/failure; pending subtitles/interaction VM |Several stale publications/replacement disturbances |All50-cycle controls pass |
|Nodes, picker cancel/success, local rollback, shared-model failure/fault, danmaku continuation, engine callbacks |25 of39 player case groups fail overall |39 of39 groups PASS; source-preserving fault/cancel controls pass |
|Three settings refresh / one later effect after50 loads |50 bold handlers/50 updates from one toggle |Every registered logical handler≤1; all three current values refreshed; one write/effect |
|50 complete actual renders, retained/read outputs |50 undisposed objects for each of the five owned types |0 for each;50 output graphs usable; shared-device closes0 |
|23 graphics faults / two genuinely suspended faults / async order /50 parameter-output traces |All controls execute; ownership groups fail |All five graphics groups PASS, exactly-once reverse release, original exception object retained |

The final owning stage prints241 aggregate checks: original240 plus one graphics dispatch check. The39 lifetime case groups and per-render/fault assertions are additional internal checks, not replacement assertions or new runner stages. [behavior-summary.json](implementation-evidence/behavior-summary.json) records both sides. Original/final raw logs are `/workspace/work/video-memory/player-fix/original-red.log` and `fixed-green.log`. Missing/ambiguous extraction anchors independently reject with exit1. `git diff --check -- src/BiliLite.UWP/Controls/PlayerControl.xaml.cs plan/tests/playersess` exits0.

The root model is explicitly DispatcherTimer.Running, not process RSS. NoInlining setup returns only weak references; request gates complete and creator locals/tasks/frames are gone before test-only collection. Stopping modeled roots releases active controls. The dropped settings event graph collects. GC/finalizer waits are used only in weak-reference fixture assertions, never product code or graphics testing.

## Final affected selection

```bash
bash plan/tests/run-all.sh --tools-root /workspace/work/maintenance-foundation/quality \
  --only batch:d1 --only batch:playertoolbar --only batch:ux1c \
  --only batch:ux3 --only batch:ux5 \
  --only LM01 --only LM02 --only LM03 --only LM04 --only LM05 \
  --output /workspace/work/video-memory/player-fix/selected-final-report
```

Exit0;10 logical/10 executable PASS; `PARTIAL_PASS`, `complete_required=false`. This selection covers changed player/danmaku wiring, surface/toolbar/sequence behavior and local-media/guard contracts. Exact private report hash and compact [selected-summary.json](implementation-evidence/selected-summary.json) are bound. Owning playersess is the separately recorded final actual-source run. Development bring-up/comparison iterations and an earlier selection were used while adding missing same-reference/public/interaction controls; the final receipts above supersede them. No complete aggregate, runner14 or package15 rerun was performed. Root's independent stable common51/52 gate remains required.

## Limitations and handoff

Linux SDK8.0.427/runtime/ref8.0.33 are available. Launching the specified `powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump -Restore` is blocked by missing `powershell`; no Windows UWP/MSBuild/device runtime or restored UWP Win2D assets/lockfile exists here. Accepted read-only metadata evidence establishes IClosable for all five types at Win2D1.26.0; the source's transitive dependency is only a lower bound (>=1.26.0), not an observed Windows resolution. No package/version/manifest was changed. [capabilities.json](implementation-evidence/capabilities.json) records exact observations and unable-to-launch command.

Native RAM/private commit/working set/GPU/handles, actual decoder/graphics/cache behavior, wrapper finalization and subtitle pixels remain unverified. Native disposal exceptions remain outside the model: the drawing-session fault double marks itself closed before throwing. R4 cleanup isolation, SMTC/display balancing/adaptive/native-stream policy and BR-21 attribution remain outside this repair. No native memory reduction, pixel equivalence, Windows cache execution or user50-video leak reproduction is claimed.

Independent implementation/fresh review may now read all frozen inputs. Then root coordinates one complete stable source gate and serialized later publication/documentation. Windows follow-up must bind actual resolved Win2D/build/package and execute the accepted warmed50-cycle/subtitles-off/on/idle/device visual protocol in the measurement workstream; those results must be separate from this source lifecycle PASS.
