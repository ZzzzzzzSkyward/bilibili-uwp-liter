# Player implementation review evidence

Verdict: **CHANGES, PF1/P1**. Review-only private directory: `/workspace/work/video-memory/player-final-review/`. Read-only product baseline HEAD `7356b94893377558191b0d70576a52bae704c1f9`; frozen current PlayerControl source SHA256 `8682d1505752a2dfd99e835259d45429cf78ee3e358caf39553b1375a7d4a3cc`. Original source SHA256 `18fac7cc1afcdf38f5896364f1606dd05446358965f3aae51cff00a7929b1e4f`.

## Inputs and independent extraction checks

Read accepted-requirements-binding, execution/implementation-review dispatch, accepted resource requirements/RR1/review, resources execution and all implementation-evidence maps/receipts. Source/test inspection covered complete diff and affected control methods, all three settings loaders, all ten released files, actual LocalMediaTrackSwitchCoordinator, session/load guards, actual child InteractionVideoVM and actual public/cache callers. Existing Season ownership and disjoint tools work are not part of this source receipt.

Private `binding-check.json` records exact copied current files, unchanged history/session/local-media/source dependencies and compiler/runtime hashes. Ten released changed/new hashes match bindings.json. Reconstructing original Program.cs by deleting only its four additive lines/blank line matches baseline bytes exactly. No old assertions or three historical history fixture files were edited. All files used by the private snapshot were byte-identical to the corresponding scoped current input. Read-only git show obtained the original Program bytes; no ref/index operation occurred.

`extraction-and-receipt-check.json` independently validates every complete extracted method against original/final actual source: original38/final39, including the newly present current-work helper. Every extracted method hash matches its source body and generated-output hash. The actual coordinator body uses only the declared LocalPlayInfo→LocalInfo type substitution; selection/model/API/native boundaries are explicit doubles. The extractor's absent/ambiguous-anchor rejection controls and exact released extractor hash were inspected. The original graphics reference body is a byte copy of the actual original method, not the new implementation.

## Owning-stage replay

The private snapshot contains only required player-stage inputs, linked services/helpers and source-wiring read targets. No source/test output was written to the repository. Exact commands, source hashes, environments, cwd and exits are in `owning-stage-commands.json`; relevant form:

```bash
UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality \
PLAYER_LIFETIME_SOURCE=<bound-original-or-private-current-source> \
PLAYER_HISTORY_SOURCE=<same-source> \
PLAYER_LIFETIME_EVIDENCE_DIR=<private-original-or-final-extraction> \
bash plan/tests/playersess/run.sh
```

Cwd: `/workspace/work/video-memory/player-final-review/snapshot`. Logs: `original-owning-stage.log`, `final-owning-stage.log`. Same fixture/stub/extractor/tool bytes for both sources. Original exit1 is intended behavioral failure, not compilation failure;25/39 lifetime groups fail. Final exits0,39/39 groups PASS, and prints `全部通过:241 项` (original240 plus graphics dispatch). Shared graphics controls pass in that run; their separate reviewer owns the G1 conclusion.

| Decisive release cases replayed | Original → final |
| --- | --- |
|50 late info closes, dispatcher-root model |50 timers/50 survivors →0/0. |
|50 late quality successes |50 opens/current sessions →0/0. |
|50 late quality failures |50 stale dialogs →0. |
|Active50 / ordinary-close / empty-late / subtitle-pending close |Expected50 active roots retained; closed settled controls0 on both sides. Clearing modeled roots releases active controls. |
|Terminal public/live/fresh/same-reference |Original unsafe; final full public active chain works, old terminal entries quiet. |
|Old info/quality distinct/same-reference, nodes/online/picker/local/danmaku/engine |Final all released39 groups pass; actual methods and declared boundaries. |
|All-three settings50 external refreshes |Original50 subtitle handlers/effects; final handler counts≤1, refreshed values and one later persistence/render effect. |

Weak-reference factories are NoInlining and return only references after gates/tasks settle. Static DispatcherTimer.Running is an explicit model; no native dispatcher/COM root count or working-set/RSS value is inferred. UI element doubles deliver value/selection/toggle callbacks; engine calls accept actual tickets but allocate no media. Current control fixture does not execute native child-VM internals; item replacement checks detach the original VM at real binding sites, while broader residual concurrency remains a separate source audit.

Ten-stage selected receipt `/workspace/work/video-memory/player-fix/selected-final-report/results.json` hash matches `d9efc95366376a49b58415589aafcfbe50d923cbd91ebf1741b1e69d9a11894e`:10 PASS, PARTIAL_PASS, complete_required=false. No repeat selection/full suite was run by this reviewer. This is not the final common51/52 gate.

## PF1 actual-source regression proof

Private project `track-ui/TrackUi.csproj` compiles the same complete original/final control slices, actual coordinator and real session/load guards. It reuses the released stubs, removes their empty UpdateLocalTrackSelectors boundary and adds that **complete unchanged actual method**. Only the option-list dependencies are empty arrays for isolation; those helpers never set enabled state. No UI enabled-state repair, private lifecycle reset or copied generation algorithm is present. Product source search confirms the selector IsEnabled assignments exist only at switch start/finally; UpdateLocalTrackSelectors does not enable them. Next also has separate transition helpers, so the required repair must respect current transition ownership.

`track-ui/binding.json` records the complete selector-method hash/source hash and exact model limits. `track-ui/commands.json` records both runs. Command form:

```bash
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/video-memory/player-final-review/track-ui/TrackUi.csproj \
  -p:ReviewSlices=<private-original-or-final-extraction/PlayerLifetimeSlices.g.cs> \
  -p:ReviewCoordinator=<matching-private-extraction/TrackSwitchCoordinator.g.cs>
```

Both runs start manifest track switch A with a controlled pending open, release the engine boundary for subsequent opens, initialize distinct manifest B through complete actual public methods, then complete A with failure. B opens once normally; no stale reopen occurs. Observed UI tuples are video selector/audio selector/Next:

```text
original exit0:
track-start ui=False,False,False
replacement-ready ui=False,False,False opens=2
old-switch-settled ui=True,True,True opens=2

final exit1:
track-start ui=False,False,False
replacement-ready ui=False,False,False opens=2
old-switch-settled ui=False,False,False opens=2
ASSERT_REPLACEMENT_TRACK_UI_ENABLED_FAILED
```

The independent assertion passes original and fails repaired source. This identifies the specific new regression introduced by guarding the obsolete finally without transferring enabled-state ownership to SetPlayItem/B. The released TrackClose and TrackSharedModel tests record a publication count before completing A and require no more writes; that safety assertion is correct but lacks an independent B-usability assertion. Restoring the old unconditional finally would pass this small test while violating B-current-switch safety, so final acceptance must include both current-owner readiness and obsolete-owner noninterference cases specified in final-review.md.

The fixture contains only managed UI/media boundaries. IsEnabled transitions come directly from actual source and do not require Windows rendering/playback to establish this regression. No native API semantics or bytes are claimed. Original/compiled-output warnings in the small targeted fixture concern unused selector dependency fields and do not determine its behavioral result.

## Custody and limits

Only assigned resource final-review/evidence and exclusive private scratch were written. All frozen scoped source/test/dependency hashes were rechecked unchanged after independent runs. No whole-tree-stable or common integration claim is made; disjoint tools may still change. Product/native build remains blocked in this Linux environment; actual resolved Win2D/UWP assets, package execution, decoded playback, pixels, cache/SMTC/display behavior, RAM/private commit/working set/GPU/handles remain unverified. No native RAM saving, leak attribution, production forced GC or graphics-policy/native-resource expansion is requested. PF1 returns to the original player writer under a later root grant.
