# Independent season implementation evidence

Verdict: **PASS / RELEASED**, no P0/P1/P2 findings. Read accepted-requirements-binding.md, implementation/execution dispatches, accepted managed requirements/review/terminal decision, managed/execution.md, implementation-evidence/bindings.md and all released actual diffs/fixtures. Wrote only this file, `final-review.md` and `/workspace/work/video-memory/season-final-review/` scratch. No source/test/main-doc/ref edits, source publication or other-owner stage runs.

## Exact acceptance scope

Baseline HEAD is `7356b94893377558191b0d70576a52bae704c1f9`. Frozen current product hashes:

| Product | SHA256 |
| --- | --- |
| `src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs` | `c91ceea2e58aa8318311841eaf22ef473711710c6222289baa6bed9b79bf4f5d` |
| `src/BiliLite.UWP/Modules/Detail/SeasonDetailVM.cs` | `615a4c04bb0f4077ff63976e993f013a455b6fcf55a77fc798cef0a9d5acc596` |

Frozen fixture hashes:

| `plan/tests/season1/` file | SHA256 |
| --- | --- |
| `Program.cs` | `ef5f606b8041dfc3c04ca27e0945abf5c86ecc9adc98308b57f87ef654a23766` |
| `Season1Probe.csproj` | `5cd19422582bbe811c9cd3d265862a4f8b96c0fa005f810c8663b45d97c4bcb5` |
| `Season1Stubs.cs` | `e82aba6606d5760608d4a54f37c0368867ab71ba310e46d0251f301335a16c9a` |
| `run.sh` | `9fd4aceee8d478aeb734c00871e7d83d0470ef15335523e51644f967a54abd8b` |
| `extract-lifetime.py` | `9945c8583bbb1dfdc8d5eead112a6be2b0920fb4f3939251274982b5d2d0a204` |
| `SeasonLifetimeStubs.cs` | `54c9def4488b4a5801b961b999c7ce55e2a85461c838fe9c349b08b35e373354` |
| `SeasonLifetimeProbe.cs` | `45e9fe8550fe53ea0d09b1de3c0dd0151442dca626e9618fac43ae52cc759f08` |

Current managed requirements hash `d82395fdaebd5a845536776de7cc78dbb40a3a297d629ca8d2afc8384a90ca39` and design review hash `78bbdf106ac3423fc562e80e6f22a8196ecb0339fbd29dbf4aa168fababd473f` match the accepted binding. Scratch `audit-bindings.json` binds execution/evidence Markdown, all seven fixtures and all12 declared old/current source inputs, including the unchanged eight VM/parser dependencies and actual MainPage/MyFrame. All released receipts named in implementation-evidence/bindings.md were hash-checked. Other owners' PlayerControl/playersess/tooling bytes are excluded from this acceptance and may continue changing; no stable-whole-tree claim is made.

## Product and fixture review

| Actual location | Checked result |
| --- | --- |
| Season page fields, SubscribeClosedPage58–66, SubscribeShare76–82, Close93–114 | Private page generation and terminal latch; latch/generation/VM invalidation precede notifications/teardown. Existing unsubscribe/share release/directory/fullscreen/miniplayer/dispose sequence retained. Later subscriptions and repeat close safely return. |
| Season OnNavigatedTo122–165, Init174–196, refresh785–789 | Closed navigation returns before base/subscriptions/work; active navigation errors log. Complete Init checks captured generation after VM await before any suffix, quietly rejects stale success/fault and logs an active suffix error. Refresh Loading guard is unchanged. No VM replacement or disposed-instance reactivation. |
| Season VM86–174 | InvalidateLoad is notification/request/clear-free. New load, all three awaits, next endpoint, accepted model/list/visibility notifications, catch/error and finally use the captured generation. Local metadata normalization/list merge precede Detail assignment. Reentrant notifications cannot continue stale publication. Active failure and last-good behavior remains compatible with existing source. |
| Unchanged page play-info/episode/preview/title methods; actual MainPage.ClosePage/MyFrame.Close | Complete method bodies run, not copied conditional logic. Page player/comment/QR/review/directory consumers count observable effects. Actual shell clears children, invokes real frame event → actual page handler/close, then detaches/removes tab. |
| Program.cs and original season1 stubs | Program is exactly original after BOM normalization with only its final return changed to call the additive fixture. No old assertion/case changed. Optional transport/decode hooks default null and reset null; old response parsing/service behavior stays intact. |
| Project/run/extractor | Source-root selection controls every direct real VM/parser link and all extracted bodies; same final fixtures are used for both roots. run.sh extracts before invoking the existing project. Aggregate runner still invokes this existing run.sh; no inventory/stage change. Extractor reads no PlayerControl, uniquely locates19 full field/method bodies, scans strings/comments and never rewrites method text. |
| Lifetime stubs/controls | Only platform, transport/services and declared side-effect boundaries are doubled. No generation/closed-owner algorithm is copied into a stub. PlayerBoundary intentionally counts post-Dispose attempts. The wrapper constructor creates real VM fields for the mechanism test and does not execute/reconstruct XAML. Review MediaID is a plain property in actual source too. |

No new product helper/registry/dependency or parser/transport/app-project/version/history change is in this scope. New product syntax uses existing UWP-compatible C# constructs. The source/fixture structure is scoped and has one execution owner; no unsupported duplicate ownership or dead product mechanism was found. Native compilation is still a separate unavailable gate.

## Independent original red → repaired green

Reproduction command: `python3 /workspace/work/video-memory/season-final-review/reproduce.py`. It first verifies released source/fixture/receipt hashes and original assertion equivalence, then makes two exclusive source snapshots. The exact same final seven fixture files are copied to both; no fixture/business method is edited. Each source root is freshly extracted and compiled with SDK8.0.427 csc, runtime/ref8.0.33, four C# fixture files, complete generated methods and the nine directly linked real VM/parser sources. Newtonsoft.Json bytes match the executor's fixed-binary dependency. Compiler response files and all outputs remain private. This is a focused component proof, not an aggregate or another owner's stage.

| Fresh private command/result | Exit / observed result |
| --- | --- |
| Original extractor and csc compilation | 0 /0. Generated C# and19-entry manifest byte-identical to released original artifacts. |
| `dotnet original/plan/tests/season1/bin/Season1Probe.dll` | **1**;141 historical PASS, additive200 PASS /297 genuine intended-safe failures. Metadata closes:50 late next requests,400 late VM notifications,50 each title/review/player-attempt/comment/QR/directory effect. Episode closes:0 late next request,400 notifications,50 each suffix effect. |
| Fixed extractor and csc compilation | 0 /0. Generated C# and19-entry manifest byte-identical to released fixed artifacts. Only the four declared unused-event/field warnings. |
| `dotnet fixed/plan/tests/season1/bin/Season1Probe.dll` | **0**;141 historical PASS, additive497 PASS /0 FAIL. At each50-close gate:0 late next requests,0 notifications,0 each title/review/attempted-player/comment/QR/directory effect. |

Scratch `run-status.json` records exact commands/exits and log hashes. Fresh original/fixed proof hashes are `7868d2ce4f1865753705791aee01b4acffef899326651e160e2fba77df173505` / `c2f0e17dcda8386fa4f17d6456980b833c9a6f258726b3eff85e249bc8cf0868`. Compiler warnings are separate in reviewer logs; after stripping the executor's compilation preamble, **all assertion stdout matches both released logs exactly**, not only totals.

`python3 /workspace/work/video-memory/season-final-review/audit-bindings.py` exits0. It independently uses recorded source ranges/anchors to check every extracted body's hash and exact generated membership for all19 entries on both sides, verifies current frozen component bytes, and runs private missing/ambiguous-anchor children. Both children exit1 with no generated output. It also confirms unchanged accepted normative hashes and exact assertion stdout equality. No source generation or build output was written back to the repository.

## Coverage and ownership interpretation

- Both50 actual-close batches independently hold metadata or episode requests, release them after close, inspect all VM notifications and side effects/next endpoint counts, settle/reset gates and return creator frames before weak collection. Metadata-decode invalidation is held separately. Closed parse/rejection/transport faults are quiet; active metadata/episode faults retain logging/error-panel semantics.
- Same-VM direct and explicit twice-invalidated A/B reverse cases cover each request gate and old success/fault, preserve B's object identities/loading/error/notifications, then accept C. Complete page reverse-operation cases retain the VM field and reject A's suffix. Fresh same-content B is a distinct page/player/VM, with no old late effects; closed Init/OnNavigated/Loaded/refresh entries cannot request or subscribe. Live refresh/retry/Loading guard and retained forward reentry stay positive.
- Real PropertyChanged invalidation at loading/model/list/visibility boundaries rejects later work; actual reentrant shell close during Detail publication prevents remaining VM/page work. An actual page/player-boundary failure logs once and does not escape the modeled async-void boundary. Repeated close remains idempotent.
- The static shell and view-share publisher are deliberately rooted. Positive50 active owners and negative50 detached-without-close share subscribers remain alive; actual close removes subscribers and yields0/50 settled owners. Non-inlined creator/close/drain frames, cleared static transport hooks and weak-only counting avoid incidental JIT/local roots. GC is test-only. These are managed topology/reachability results with doubled platform ownership, not Windows native roots or RAM savings.

The executor's bound selected report `selected-report/results.json` (`100b3e3086a94b74b384d89e2bc2bc54769ab510d54f84d0987bc2d4e8b4a9e0`) reports3/3 playersess/stab/ux3 PASS, **PARTIAL_PASS / complete_required=false**,51 logical/52 executable inventory. The original playersess240 and historical selected source wiring were checked in that receipt. Current PlayerControl/playersess are another owner's in-flight work, so this reviewer did not rerun those stages or promote their earlier snapshot result to current common integration.

## Limits and release

Source/extraction/fixture proof establishes the bounded M1 lifecycle repair. No Windows VS/UWP build, XAML/COM/cache/Loaded scheduling, constructor/native event delivery, decoded media, display/SMTC/dispatcher/decoder/GPU resource release, actual watched50-cycle journey or Windows process RAM measurement was performed. The executor's environment JSON null shell entries describe command discovery in that receipt; they are not used here to infer absence of every portable PowerShell installation. BR-21/native attribution and the global player/background work remain measurement-gated.

No requirement was relaxed. Scope PASS is released for root's later residual audit, all-owner freeze, common integration, documentation and serialized publication gates. Native/cache behavior remains unverified under the accepted fresh-owner source preconditions.
