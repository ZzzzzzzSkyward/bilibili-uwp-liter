# Independent managed review evidence

Verdict: **PASS / RELEASED**, amended requirements only. Review scratch: `/workspace/work/video-memory/managed-review/`. Product/tests/build/main docs/refs and all requirements were read-only for this reviewer. Baseline HEAD/branch remained `7356b94893377558191b0d70576a52bae704c1f9` / `dev`; protected-path diff is empty. Only this file and `review.md` are repository outputs of this reviewer. The narrow terminal-owner amendment is accepted below; the initial source/proof evidence remains historical and unchanged.

## Source checks

| Actual source | Reviewed behavior |
| --- | --- |
| `Pages/SeasonDetailPage.xaml.cs:35–173,175–291,587–608,762–766` | Constructor keeps a single VM; XAML uses its identity. Loaded subscribes idempotently. Close unsubscribes/share-releases, changes state, resets directory, exits fullscreen/miniplayer and disposes. OnNavigatedTo awaits Init; refresh also awaits Init. Complete Init has no close/generation check and calls actual title/review/play-info/QR path. Both episode and preview paths initialize player before comment loading. |
| `Modules/Detail/SeasonDetailVM.cs:86–147` | Metadata request → metadata GetResult → episode request/parser → mapper/publication. No close/replacement identity. Ep-list refusal leaves last good object/list identities; normal header failure logs and falls back. Catch calls real `HandelError`, which can log, then publishes error; finally unconditionally changes Loading. |
| `MainPage.xaml.cs:96–114,130–144,191–202`; `Controls/MyFrame.cs:6–14` | Each normal tab navigation creates its frame. Ordinary tab close clears Grid children, invokes actual frame notification, then detaches/removes tab. Frame Close is an instance event, not a static page registry. |
| `SeasonDetailPage.xaml.cs:151–156,268–275`; `NoTabMainPage.xaml.cs:189–289`; `Pages/BasePage.cs:8–45` | Forward navigation releases share/close subscriptions without ending a cached page's playback lifetime; Back/Blank invokes close. NewInstanceFrame pauses intentionally retained outgoing PlayPage and removes Back topology according to actual source. No minimize/visibility invalidation is implied. |
| `Controls/PlayerControl.xaml.cs:1066–1076,1797–1832,3920–3959` | Init dereferences dispRequest before opening; actual Dispose clears it synchronously before optional history await. Subtitle Tick does not self-stop for closed/End state. This review does not substitute the resource owner's timer model for Windows dispatcher evidence or approve PlayerControl edits. |
| `Modules/Detail/VideoDetailVM.cs:151–221`; `Pages/VideoDetailPage.xaml.cs:326–359` | VM may keep requesting/publishing after close; actual immediate page post-await guard rejects closed/wrong-id/stale-transition work. Discovery compiles only that declared prefix, not the full video presentation suffix. |
| `Services/ApiCapture.cs:27–48,140–169`; `Helpers/VideoPlayHistoryHelper.cs:10–16,27–108`; `Modules/Player/PlayerVM.cs` | Capture defaults off and queues full response arguments behind a static semaphore only when enabled. A/B scalar dictionary trims on explicit startup cleanup and serializes snapshots; Set is not capped. PlayerVM has no static page registry/timer. These are conditional candidates, not measured native leaks. |
| `plan/tests/season1/Program.cs:28–49,81–177,180–540,550–583`; actual parser/mapper/DTO | Original tests cover real envelope/parser/mapper/VM behavior, metadata fallback, empty/null/error categories, order/classification/deduplication/status and last-good identities. The proposed generation repair does not require relaxing them or changing these parse layers. |

Hashes of the **initial** frozen review inputs and relevant source/test files are in scratch `review-bindings.json`. Historical inputs (current amendment hashes are below):

- `managed/requirements.md`: `2e013c5cfae48a2064debd72c158e43f07249b319a314dbe98d7f4d33dd158b4`
- `managed/evidence.md`: `3ada825428cdd963caadcc581662220329e369170e0a6c5d7108a62efb7dce74`
- `managed/source-proof.md`: `b6502e161ca9c06ca65cee89ed770f4ee4a018f0d160c00d4325810985f6cb22`
- Whole Season page: `b44706b9156259cc57acda3611023b4a9e0ed96f9345b3576ddaac3148804490`
- Whole Season VM: `a9b84b5512e04f2081d34b9f9d5642a9f5aee902f01b476881996fc9c3c1a07b`
- Whole PlayerControl: `18fac7cc1afcdf38f5896364f1606dd05446358965f3aae51cff00a7929b1e4f`

## Original independent reproduction — unchanged

`python3 /workspace/work/video-memory/managed-review/reproduce.py` copies fixture inputs into the reviewer's exclusive scratch, changes output paths only, freshly regenerates/extracts and compiles. Product methods remain unmodified. It independently verifies all **25** manifest source hashes, source ranges/body hashes and complete-method membership; the one video prefix is separately declared. Fresh ProductSlices and extraction manifest are byte-identical to discovery. The nine directly compiled real-VM source hashes also match. `bindings.json` and `run-status.json` bind results/commands; compile logs contain only the documented five/two unused fixture warnings.

Exact compiler: `/workspace/work/maintenance-foundation/quality/dotnet/dotnet` running `sdk/8.0.427/Roslyn/bincore/csc.dll`, optimized .NET8 build with 8.0.33 ref pack/runtime. Full-real-VM fixture uses Newtonsoft.Json13.0.3. No NuGet restore, live network or UWP media occurs. Real-VM stubs differ from existing season1 stubs only by BOM removal and the declared async transport hook; business parsing/mapping/publication remains product code.

| Fresh command under review scratch | Exit / result |
| --- | --- |
| `dotnet csc.dll @compile.rsp` | 0; freshly generated actual method slices compiled. |
| `dotnet ManagedMemoryProof.dll` | 0; 30 descriptive assertions. Active Video/Season tabs retain50 and50 share subscribers; actual close gives0 subscribers/0 survivors; intentionally omitted notification retains50 until real close; Local owner cycles collect; NoTab Back returns one home frame/0 outgoing survivors. Pending video/season pages retain50 until gates drain, then0. |
| `dotnet ManagedMemoryProof.dll --require-safe-season` | 1, expected genuine baseline failure. Fifty actual season page continuations republish/enter disposed actual player initialization;50 null dereferences,0 successful media opens. Desired no-fault assertion fails. |
| `dotnet csc.dll @real-season-vm/compile.rsp` | 0; full real Season VM/parser/mapper/envelope/model closure. |
| `dotnet real-season-vm/RealSeasonVMProof.dll` | 0;5 descriptive assertions. Active owner publishes valid episode;50 metadata-held and50 episode-held closed VMs each republish50/50, with100 calls per scenario. |
| `dotnet real-season-vm/RealSeasonVMProof.dll --require-safe-vm` | 1, expected genuine late-publication failure. |

All four execution-log SHA256 values match discovery exactly: proof `410221e317090121e89288fa873dfb05d29ffe15455f12d5bcf5bb23284c8ed9`, page red `90d6450ad74ed97a6b6fd3d612fc121eb44029b7eec4aa8fc1bb81ff901a208d`, real-VM proof `5f199d89c43b25e79843512057b16494c568526ce33288bac47b9745693166ea`, real-VM red `ca25b07cdb2ed097db0f48c74fbf323821db35c2e4391136a797a5c697467f0b`.

Weak-reference interpretation was inspected, not inferred from RSS: static shell/view publisher stay rooted; creator/close/drain/count helpers are non-inlined; strong observed targets exist only in returned helpers; task/gate lists clear before settled collection; counting frame retains WeakReference objects, not TryGetTarget locals. The omitted-close and active controls demonstrate a detectable external root. Test-only collection is outside production. Platform stubs omit native wrappers, actual dispatcher/cache behavior and live network durations.

## Additional reviewer controls

1. `python3 /workspace/work/video-memory/managed-review/check-boundaries.py` exits0. In `page-boundary/`, only the player initialization boundary is replaced by a no-throw call-count spy, and the empty QR stub records calls. Every extracted season page method remains exact and is checked for membership. Fifty pending Init operations are closed through actual shell/frame/page bodies, then released: **title50, initialize50, comment50, QR50, review50; share subscribers0**. Page references are intentionally held for observation; this is a publication control, not reachability/native evidence. It independently confirms that player exception suppression cannot establish season safety.
2. `python3 /workspace/work/video-memory/managed-review/check-vm-order.py` exits0. `vm-order/` directly compiles the unchanged full real VM closure with the same transport/services stubs. Distinct payloads1/2: B publishes first, then A overwrites B at both metadata and episode gates. In close-state-A → fresh-B → stale-A-fault order, B's model stays but stale A sets ShowError and logs. These are descriptive baseline failures supporting required generations/quiet stale error handling; no private green product variant was created.

The proposed original-source safety regression must remain red on baseline and become green against the eventual accepted source without retaining descriptive defect expectations as success conditions. The page-boundary spy is permitted evidence for page publication; it is never proof of actual PlayerControl reopening/native playback. Resource R1/R2 and terminal public-entry/fresh-owner behavior require the resource owner's separate exact-method acceptance. The amended requirements preserve that ownership and demand season acceptance independently.

## Terminal-owner normative re-review

Read the active managed-revision-dispatch.md, root lifetime-decision.md, released managed/lifetime-reconciliation.md, revised managed requirements/evidence, and resource RR1's revised terminal contract. No executable, original proof or suite was rerun for this wording amendment. Read/hash verification is recorded in scratch `terminal-amendment-bindings.json`; it checks the ten reconciliation source hashes, original relevant product/season1/source-proof bindings, the six boundary artifacts and the unchanged original reviewer execution logs. All match. Protected source/test/build/main-doc diff remains empty.

Current SHA256 bindings:

- Revised `managed/requirements.md`: `d82395fdaebd5a845536776de7cc78dbb40a3a297d629ca8d2afc8384a90ca39`
- Revised `managed/evidence.md`: `3875817369edc4ae2e7e720e8602717b46ef03d862b1cdcfd7289fb417b94d13`
- Unchanged `managed/source-proof.md`: `b6502e161ca9c06ca65cee89ed770f4ee4a018f0d160c00d4325810985f6cb22`
- `managed/lifetime-reconciliation.md`: `cf5d001a7df0c878ed9f840516a2a461c85cef3f35386d4276b30fe279dfcdc0`
- Reused boundary `Program.cs`: `b6cc6a5fe0f4b2baacb36b27774507fe726e830dfa9f73b5032f7babf5d70995`; `LifetimeBoundary.dll`: `4664fec02aa5e0338fe4054a20a642ce434359198aa1674f9ed69c9a5007d3bd`; `proof.log`: `35ed733fc867f413975c3e508f9218a5c5ca160404c6562c63c7c5d1f5cc60b2`.

The reused `/workspace/work/video-memory/managed/lifetime-boundary/Program.cs` references the originally bound actual-method assembly and full-real-VM assembly. Inspection confirms no private product flag reset, no product method replacement and no claim of restored SetPlayItem/native resources. Its recorded six assertions establish two accepted initializations on a live control using the same reference, actual post-Dispose initialization failing before another SetPlayItem boundary call, a distinct fresh control accepting that reference, two complete Season Init effects before close, and sequential A/B loads on the same full real VM. Those sequential controls **do not prove invalidation, reverse-order safety or closed-page rejection**; the revised contract still demands those meaningful original-source red → repaired green tests.

| Revised control / precondition | Independent review result |
| --- | --- |
| Same VM A pending → InvalidateLoad → B → A success/failure last; direct reverse-order loads with distinct payloads | Independent reusable generation contract is explicit. The old source has no InvalidateLoad API and earlier reviewer controls demonstrate stale overwrite/error. No closed page/player is revived and no VM field is transplanted. |
| Same live page/player refresh/retry before Dispose | Actual Season handler762–766 retains `if (seasonDetailVM.Loading) return`; XAML82/99/369 wires normal/menu/error retry. Init and episode/preview changes reuse a live player without Dispose. Normal and active-error/last-good behavior remains required. |
| Closed old page/player A versus distinct fresh B, then late A | Terminal close must preserve old latch; old A has zero model/title/review/attempted-player/comment/QR effects, B starts once and keeps its state. Same content/id/reference never authorizes reviving old A. |
| Separate stale Init/OnNavigated entry on closed page | Revised minimal contract explicitly returns before subscriptions, another request or UI/player work and forbids clearing the latch. This is a required final behavioral assertion, not a claim that baseline already rejects it. |
| Tabbed and no-tab shell/cache | Actual MainPage96–114 creates new MyFrame;191–202 detaches closed tab;130–144 closes outgoing page before mouse Back. BasePage15–23/Season268–275 disable/close outgoing Back/Blank. NoTab XAML20 really uses NewInstanceFrame; actual duplicate type creates another frame, Back disables outgoing cache then clears forward stack or removes extra frame. MainPage does not clear ForwardStack, so no stronger empty-native-cache claim is made. |
| Multiwindow and other callers | Actual NoTab153–173 and MessageCenter617–639 create new view/frame; Consolidated navigates Blank before closing. Only product GoForward call found is WebView's web history. Video242–246/Local98–104 reset code and cached-reentry comments do not reconstruct constructor-only resources; no supported same-disposed-control restart is established, and no Video/Local change is granted. |
| Safety/ownership preserved | Full no-late publication and zero attempted player initialization remain decisive even with a quiet terminal PlayerControl guard. Existing source hashes/old assertions/50-close controls are unchanged; no XAML/control reconstruction, VM replacement or timer/display/toast/event restoration is introduced. PlayerControl/playersess remain under the resource writer; Season page/VM/season1 remain disjoint. |

**Native cache/Loaded delivery is unverified**, as are real constructor/event/display/SMTC/decoder behavior, playback and RAM/GPU outcomes. The traced source policies are explicit preconditions for the chosen terminal contract. An actual supported Windows path returning a terminal page/control requires recorded source/native evidence and a separately reviewed routing/restoration amendment. This review does not pass that unavailable native gate. No P0/P1/P2 amendment remains; reviewer custody is released pending root's all-final-PASS gate.

No unchanged aggregate suite, Windows build, actual50-video watch/close run or native memory capture was performed for this Markdown review. Source design PASS does not close those gates.
