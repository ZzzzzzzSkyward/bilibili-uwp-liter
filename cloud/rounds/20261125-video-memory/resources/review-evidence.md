# Resource requirements re-review evidence — PASS / RELEASED

Current status: **PASS; RR1/P1 resolved, no remaining requirements amendment.** The original first-review proof below remains historical evidence; its former CHANGES status is superseded. Current re-review scratch: `/workspace/work/video-memory/resources-review/rr1-rereview/`. Product baseline remains `7356b94893377558191b0d70576a52bae704c1f9`.

Read released resources-revision-dispatch, lifetime-decision, resource requirements/evidence/RR1-lifetime, managed lifetime-reconciliation and final amended managed requirements/evidence/review. The final managed review is PASS/RELEASED and uses the same terminal old page/player versus live-owner/independent VM/fresh-owner distinction. `binding-check.json` records all revised input hashes and that linked managed review hash; all17 previous/current source/caller bindings and six new complete extracted methods were independently matched to current actual source. Seven fixture input files were copied byte-identically to private scratch. Original discovery extracted bodies/copies remain unchanged.

## Narrow independent replay

No original R1/R2/R3/240-check suite or full suite was rerun for this revision. Only the new public-boundary fixture was compiled/replayed in private scratch, with exact commands/exit codes in `commands.json`:

```bash
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/video-memory/resources-review/rr1-rereview/probe/ResourceProbe.csproj \
  --configfile /workspace/work/video-memory/resources-review/rr1-rereview/probe/NuGet.Config
/workspace/work/maintenance-foundation/quality/dotnet/dotnet \
  /workspace/work/video-memory/resources-review/rr1-rereview/probe/bin/Debug/net8.0/ResourceProbe.dll \
  --require-terminal-public-entry
```

Descriptive baseline exit0; desired terminal run exit1. Logs: `observed-public-boundary.log` and `desired-terminal-public-red.log`.

| Case | Independently observed original-source result |
| --- | --- |
| Live owner Unloaded→Loaded, no Dispose |1 open,1 Play boundary call,1 subtitle timer,1 danmaku timer. |
| Complete fresh public chain |1 open,1 Play call,1 subtitle timer,1 danmaku timer,1 display-request boundary call. |
| Actual Dispose/Unloaded→old Initialize and Loaded |Both NullReferenceException; current source is not safely terminal yet. |
| Distinct fresh owner, same exact PlayInfo object |1 open,1 Play call,1 subtitle timer,1 danmaku timer. |
| Old quality pending→close→fresh owner→old result |Old post-close opens1; fresh stays1 open/1 subtitle timer. |
| Live same-reference replacement B completes before A quality |2 total opens, B subtitle timer1/Play1; required final is only B's1 open. |

The copied complete methods are InitializePlayInfo, SetPlayItem, Loaded, Unloaded, MediaOpened and LoadPlayerSetting; complete Dispose/quality/info/subtitle and LoadSutitleSetting bodies remain the original discovery copies. Real session/async-load guard classes remain linked. SetPlayItem is actual business logic, not a restoration spy. The constructor double wires child events once; actual LoadPlayerSetting reads doubled persisted autoplay=true; the fixture does not reset product private lifetime state. Network, display, dispatcher, engine, AB-history, UI, danmaku-load and danmaku-settings boundaries are doubled. MediaOpened is signaled at the engine boundary and executes the real handler; no native decoding/toast visual/display accounting/cache claim follows. Danmaku settings still being stubbed in this boundary proof does not satisfy final R3 acceptance, now explicitly all-three actual methods.

The current desired assertion is decisive for the baseline public-entry exceptions and old-quality reopens. It is not a full observation of global binding/active-slot changes; final tests must additionally observe those existing normative zero-effect criteria, preserve the fresh/live counts, and run unchanged against bound baseline and final source. Merely making null accesses conditional would not meet the accepted terminal contract.

## Caller preconditions and compatibility

Independently inspected actual MainPage new-tab/close/mouse-back, MyFrame.Close, BasePage/PlayPage cache policy, NewInstanceFrame Navigate/ContainsPageType/GoBack in NoTabMainPage, NoTab XAML and window creation, MessageCenter.OpenWindow, and Season/Video/Local Back/Blank calls to base. MyFrame.Close only raises ClosedPage; the shell's separate detach/cache-disable policy is material. MainPage mouse Back does not clear forward history; Disabled outgoing cache is the source precondition and product search finds no Frame.GoForward (only WebView.GoForward). Native Frame/XAML delivery remains unavailable, so no universal native cache eviction is claimed.

Live Season refresh uses its existing Loading guard and no Dispose; episode/preview initialization likewise reuses an open control. VM generation reuse does not revive a closed page/player. Video/Local reset code and old cache comments are not proof of resource restoration and do not enlarge those files' scope. The Video comparison retains its declared initial-detail guard coverage; later awaits such as the conditional redirect conversion are not independently proven safe by that prefix. No new Video/transport repair or broad caller-safety claim is granted from this resource review; any separate caller expansion needs complete-method reproduced acceptance. Closed old-player entries must still reject terminally regardless of caller behavior.

No conflicting supported same-disposed-control restart route was found. The requirements make future contradictory supported Windows cache delivery a specific evidence-triggered routing/restoration amendment, not permission to rebuild toast/timer/display/events now. Root's separate graphics discovery remains outside this default R1/R2/R3 source scope.

All recorded revised-input/source bindings were rechecked unchanged before releasing this PASS. Earlier actual50 R1/R2/R3/ordinary/active/root controls and original240-test byte comparisons below remain valid and were reused. No source/test/tooling/main-document/index/ref edits, native run, live account/media request or full unchanged suite occurred.

---

# Historical first-review evidence (superseded status)

Historical status: independent review **CHANGES (RR1, P1)**, resolved by the re-review above. Sole private scratch: `/workspace/work/video-memory/resources-review/`. Intake/current source HEAD: `7356b94893377558191b0d70576a52bae704c1f9`. Product/test sources remained unchanged throughout. Requirements/evidence stayed frozen and read-only.

## Binding and replay

`source-binding-check.json` records the reviewed input hashes, nine discovery source/test bindings, twelve complete extracted-method bindings, six byte-identical fixture copies and six existing playersess inputs compared with the prior240 PASS snapshot. Each copied method body is byte-identical to the current full method and matches its discovery method hash/line. The replay project links actual `PlayerSessionState.cs` and `LocalMediaAsyncLoadGuard.cs`; it does not replace their decisions in stubs.

| Actual file | SHA-256 |
| --- | --- |
| `Controls/PlayerControl.xaml.cs` | `18fac7cc1afcdf38f5896364f1606dd05446358965f3aae51cff00a7929b1e4f` |
| `Controls/Player.xaml.cs` | `97394caf6013c2588f7137ae9d267b24f7247189e418ba897336cb8ae89ac4b0` |
| `Services/PlayerSessionState.cs` | `508f5de763cd5ef4135f8fd87f7c37a3887dff8836e36f365458127a50338b3a` |
| `Services/LocalMedia/LocalMediaAsyncLoadGuard.cs` | `a46abc7a0c598b767e02e0d7b2bc4c17c89362b9d713b46b203529a56934d8e3` |
| existing `playersess/Program.cs` | `fdd1850ecbe5fbea500ffff4cbc41f375144209a8ce0d24ef39c8a6a92715838` |

Commands (full argument arrays/exit codes also in private `commands.json`):

```bash
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/video-memory/resources-review/baseline-probe/ResourceProbe.csproj \
  --configfile /workspace/work/video-memory/resources-review/baseline-probe/NuGet.Config
/workspace/work/maintenance-foundation/quality/dotnet/dotnet \
  /workspace/work/video-memory/resources-review/baseline-probe/bin/Debug/net8.0/ResourceProbe.dll \
  --expect-closed-clean
```

First command exit0 asserts observed original defects plus controls; it is not a repair PASS. Second command is the desired-clean negative assertion, exit1. Logs: private `observed-baseline.log`, `desired-closed-clean-red.log`.

| Replayed case | Cycles | Running modeled roots / settled survivors | Other observation |
| --- | --- | --- | --- |
| Active subtitle path |50|50 /50|Clearing test roots releases all50. |
| Info/subtitle completes, then ordinary close |50|0 /0|Successful normal-close control. |
| Close while info pending, late result empty |50|0 /0|Finite completed request not a settled root. |
| Subtitle pending before close |50|0 /0|Real pre-existing async guard works. |
| Info pending→close→subtitle-bearing info |50|50 /50|Desired-clean check fails; clearing roots releases all50. |
| Same-owner subtitle settings reload |50 loads|0 survivors when owner dropped|50 Bold handlers; one toggle invokes50 updates. |
| Quality pending→close→success |50|Not a reachability test|50 engine calls and50 new accepted sessions after close. |
| Actual ClosePlay ordinary / repeat |2 closes|Not a reachability test|Both modeled players disposed once. |
| Actual ClosePlay injected first FFmpeg dispose fault |1|Not a reachability test|Exception escapes and skips later releases, establishing only conditional R4. |
| Session invalidated before late Hold |50|Not a reachability test|50 late leases each disposed once. |

NoInlining setup returns only weak references; collection occurs after setup/task locals leave their frames. A static set explicitly models running DispatcherTimer roots. UI/network/engines are boundary stubs; the engine accepts the actual new ticket and counts calls without allocating media resources. History's `_hasPlayedCurrentItem=false` excludes pending network report work in this isolated fixture. Subtitle Tick is stubbed in the replay, so its real full body was checked separately: actual lines1797–1830 have no Stop/self-detach on End or any non-Playing state. The running-timer result survives this check but still does not prove Windows rooting or native permanence. No copied render/session decision in a stub is used as repair evidence.

## RR1 public reopen precondition

Private `reopen-probe` copies the unchanged baseline fixture and adds the complete unchanged current `InitializePlayInfo` method; `reopen-binding.json` records source/method hashes and boundaries. DisplayRequestStub only counts RequestActive calls. SetPlayItem is a completed-task boundary counted for the live positive control; it is never reached by the failing post-Dispose initialize. Actual Dispose remains unchanged, complete and non-suspending in this no-history case.

```bash
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/video-memory/resources-review/reopen-probe/ResourceProbe.csproj \
  --configfile /workspace/work/video-memory/resources-review/reopen-probe/NuGet.Config
```

Exit0 means the fixture asserts the observed source precondition failure, not desired post-repair reopening. Private `reopen-baseline.log` contains:

```text
case=public-initialize-active actualInitializeCompleted=true boundarySetPlayItemCalls=1
exceptionStack= ... ControlSubject.InitializePlayInfo ... ActualInitialize.cs:line 6
case=public-initialize-after-actual-Dispose exception=NullReferenceException at=dispRequest.RequestActive boundarySetPlayItemCalls=0
```

Other source facts independently read: constructor creates the readonly toast service and display wrapper at356–357, danmaku timer at366–368; XAML wires player callbacks at501; Dispose stops/nulls danmaku at3945–3948 and nulls display at3952–3954, detaches four player events at3935–3938, and disposes the toast service at3922. PlayerToastService keeps `_disposed=true` after Dispose and Show returns early thereafter. Loaded unconditionally calls `danmuTimer.Start()` at463. No same-instance reinitialization path recreates/rebinds those resources. This is a public-source API/lifetime inconsistency requiring the contract choice in RR1; it is not evidence for a native display-accounting repair.

## Actual caller and regression audit

Read complete GetPlayerInfo, SelectInitialSubtitle, both SetSubTitle overloads, ClearSubTitle, Dispose, SetQuality, ChangeQuality, PlayLocalFile, all three settings loaders, SetPlayItem and public InitializePlayInfo. Also inspected complete SubtitleTimer_Tick, UpdateSubtitle, ChangedNode, picker/import handlers, SwitchLocalTrackAsync/current-item helpers, ChangeEngineAsync, native Hold/Accept/ClosePlay paths and the actual local track-switch coordinator.

- GetPlayerInfo publishes menus/subtitles after info await and interaction/title after node await; online field assignment follows its own await without a validity check. Current item is retained on close. SetPlayItem's post-child check cannot protect these publications.
- ChangedNode mutates `CurrentPlayItem.cid` in place and resumes after two awaits into SetQuality; immutable work identity must include the operation generation, not only object reference. A gate at the final child that captures the replacement's generation as fresh cannot establish that the caller is still current.
- Pickers await before calling imports; imports call guard.Begin afresh. A close between picker start/result must prevent both mutation and fresh ticket creation. Track coordinator may perform a second rollback open after a failed/obsolete first open; reference/resume-id checks currently survive close with unchanged item.
- VideoDetail/Season/Local pages call explicit Dispose on actual close/back-to-blank paths. Unloaded only removes view/global bindings and focus timer, so it must remain different from terminal close/background playback. Season InitSeasonDetail can call public initialization after its await; M1 belongs to the separately reconciled page/VM owner.
- Read every existing playersess Program section (state, replacement, wiring, publish order, attempts and progress), HistoryCheckpointProbe cases and history-storage coverage. Required preserved behavior includes held-versus-accepted ownership, late Hold once, Abandon fallback validity,12 session-bearing opens, history key/30s timer subscription, synchronous local save, serial latest drain, pause/end/manifest behavior and flush-before-deferral completion. No original assertion was edited.
- The measurement author's existing-player-probe log reports240 checks/exit0. Its six current test inputs are byte-identical to repository files, independently compared. This review did not rerun that unchanged suite or claim its240 checks execute native media. Final additive cases must retain these assertions and the51 logical/52 executable inventory.

## Scope and limitations

R1/R2 are genuine original-source lifecycle failures under modeled boundaries; R3 is repeated work on one owned child-event graph, collectible when dropped. None establishes the user's50 actual-video native RAM slope. Ordinary ClosePlay does detach media elements/dispose wrappers; session Accept transfers ownership to Player fields intentionally. Native callback/cache/stream/adaptive/Win2D/danmaku/display questions remain candidates; clear local ownership gaps are not by themselves accumulating global roots. R4 needs its own amended fault acceptance/grant.

Linux/exact SDK can run these source fixtures. Windows UWP/WinRT media, Windows heap/graphics/handle capture and actual playback are unavailable; PATH absence does not prove portable PowerShell is absent. No native run, media/account request, production forced GC, source repair, Git ref/index mutation or full unchanged suite was performed. All nine original source bindings were rechecked unchanged after the private runs.
