# M1 season implementation — FROZEN / RELEASED for independent review

Executor: memory_season_executor, gpt-6.1-sol/high. Local source/test grant: `../execution-dispatch.md`, PARTIAL ACTIVE season/tooling only. Applied accepted managed requirements/review, terminal lifetime decision and reconciliation. Baseline/unchanged HEAD: dev `7356b94893377558191b0d70576a52bae704c1f9`. Root's startup clean sync and explicit dirty continuation apply. No Git mutation, fetch, staging, commit, ref change, publication, main documentation or other owner's source edit was performed. Local implementation and all seven season1 fixture files are frozen for read-only review; any review repair needs a resumed grant. No source blockers within this grant.

The page now latches terminal close, advances its page operation generation and invalidates the VM before existing teardown/state/player effects. Repeated close is idempotent. A closed Init/OnNavigated/Loaded subscription path cannot request or resubscribe. The complete InitSeasonDetail captures its page generation and rejects stale completion before title/review/player/comment/QR work. Active page initialization/navigation exceptions log at the boundary. Retained forward navigation stays live, and the existing refresh Loading guard remains intact.

The same VM field and x:Bind identity remain in place. VM InvalidateLoad advances only a private generation with no notifications/requests/clears; each new load also advances it. Metadata request, metadata decoding and episode response ownership are checked after their awaits. Metadata/map values remain local until acceptance, and stale catch/finally cannot publish errors or Loading. Because PropertyChanged executes inline, ownership is also checked between state publications; a callback that closes/invalidates or begins a later load cannot permit the old publication batch to continue. Normal parsing/fallback/order/empty/null/last-good behavior is unchanged. No transport cancellation or hard pending-duration bound is claimed.

## Exact changed/new files

Changed product: `src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs`, `src/BiliLite.UWP/Modules/Detail/SeasonDetailVM.cs`.

Changed existing owning-test files: `plan/tests/season1/Program.cs` (only calls the additive fixture after all original assertions), `Season1Stubs.cs` (optional transport/decode gates, null by default/reset), `Season1Probe.csproj` (source-root binding plus generated exact slices), `run.sh` (extract before compiling). New scoped files: `SeasonLifetimeProbe.cs`, `SeasonLifetimeStubs.cs`, `extract-lifetime.py`. No stage, aggregate runner/manifest, parser, transport, PlayerControl, VideoDetailVM, app project/package/asset or main document changed.

Evidence owned here: this file and `implementation-evidence/bindings.md`. Raw material is exclusively `/workspace/work/video-memory/season-fix/`; generated build outputs remain ignored `plan/tests/season1/bin|obj`. Original source/test snapshots were made before edits into `original/`, including frozen PlayerControl and actual MainPage/MyFrame. Full fixture/source/extraction hashes, anchors and original/fixed ranges are in [bindings.md](implementation-evidence/bindings.md).

## Final reproducible commands and exits

Working directory for all commands: `/workspace/bilibili-uwp-liter`. Tool root contains SDK8.0.427 and runtime/ref8.0.33. Commands below invoke the same final fixture/stub/extractor/project bytes; source-root selection is the only red/green input change.

```bash
SEASON_SOURCE_ROOT=/workspace/work/video-memory/season-fix/original UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality bash plan/tests/season1/run.sh > /workspace/work/video-memory/season-fix/original-red.log 2>&1
# exit 1: original 141 assertions PASS; additive 200 PASS / 297 FAIL

UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality bash plan/tests/season1/run.sh > /workspace/work/video-memory/season-fix/fixed-green.log 2>&1
# exit 0: original 141 assertions PASS; additive 497 PASS / 0 FAIL

bash plan/tests/run-all.sh --tools-root /workspace/work/maintenance-foundation/quality --only batch:playersess --only batch:stab --only batch:ux3 > /workspace/work/video-memory/season-fix/selected.log 2>&1
# exit 0: all three PASS; PARTIAL_PASS, complete_required=false

python3 /workspace/work/video-memory/season-fix/check-extractor.py > /workspace/work/video-memory/season-fix/extractor-controls.log
# exit 0: missing and ambiguous extraction child invocations each exit 1, no output created

git diff --check
# exit 0, no whitespace errors
```

The final owning season1 receipts bind the final original and repaired inputs; the directly affected selected stages ran after the semantic refinement. There was no full-suite run. Selected report: `/tmp/bili-offline-20261125T012830Z-97104-nszplsih`, copied to scratch `selected-report/`; results.json explicitly records PARTIAL_PASS, 3/3 selected, unchanged51 logical/52 executable inventory, complete_required=false. Original playersess240, stab and ux3 stay intact. Native compilation and Windows playback/RAM remain blocked, not passed.

Earlier bring-up/red/green runs were superseded while completing counters, async-void error reporting and synchronous notification invalidation. Final red/green receipts above use the same final fixture bytes. After the final semantic guard change, owning and affected selected stages passed; a later VM-only comment/indentation cleanup was followed by the final owning green receipt. Selected playersess/stab/ux3 do not consume SeasonDetailVM and their relevant source inputs remain exact and unchanged. No broad/repeated full suite was run. Four harmless compile warnings remain: existing unused RelayCommand events and two extracted/fixture assigned-but-unread directory/changedFlag fields; no compile errors.

## Decisive behavioral counters

| Gate / result | Actual closes | In-flight requests released | Late next requests | Late VM notifications | Late title/review/player-attempt/comment/QR/directory (each) |
| --- | ---: | ---: | ---: | ---: | ---: |
| Original metadata | 50 | 50 | 50 | 400 | 50 |
| Original episodes | 50 | 50 | 0 | 400 | 50 |
| Repaired metadata | 50 | 50 | 0 | 0 | 0 |
| Repaired episodes | 50 | 50 | 0 | 0 | 0 |

Both runs execute complete actual MainPage.ClosePage → MyFrame.Close → SeasonDetailPage_ClosedPage/ClosePage bodies and the complete actual Init/InitializePlayInfo/episode/preview/title methods. Complete real SeasonDetailVM, real parser/mapper/envelope/DTO closure are directly compiled. The player-boundary spy deliberately accepts/counts attempted post-Dispose calls rather than suppressing them; old-source failure is therefore independent of any resource owner's player fix or disappearance of a null dereference.

The additive497 checks cover active delayed metadata/episodes and exactly one suffix; both50 gated ordinary closes with zero late publication/attempted suffix/next endpoint; settled0/50 weak survivors; post-close success/rejection/parse/fault at each endpoint; independent metadata-decode invalidation; twice-invalidation without notifications; same-VM invalidation/reload and direct reverse A/B completion at metadata/episode success/fault, preserving B model identities/error/loading and then accepting C; captured page reverse completion success/fault; fresh distinct page/player/VM with same content/id versus old terminal success/fault; closed stale Init/OnNavigated/Loaded/refresh entry; live refresh/loading guard/retained forward reentry/error retry and last-good models; active faults and async-void logging; synchronous invalidation at every loading/model/list/visibility notification boundary and actual reentrant shell close during Detail publication; repeated terminal close; positive50 active tab roots, negative50 omitted-close static-share roots, actual close cleanup0/50.

All141 historical season1 assertions run unchanged before the additive fixture in both red and green, covering parsers/fallback/section order/dedup/preview classification/null/valid-empty/transport/error/toast/last-good behavior. Historical source-wiring checks still inspect the current repository, separately from the baseline-linked behavioral VM/page closure; those checks are not presented as baseline runtime proof.

## Explicit doubles, ownership and residual limits

Managed Frame/Page/Grid/TabView/Pivot/navigation/share/setting classes replace platform types. Share is a deliberately static view publisher; the shell is a deliberately static root. Actual subscribe/release and shell/frame/page close methods run atop these ownership containers. Frame doubles do not implement native UWP cache, COM, Loaded scheduling or view consolidation. Season review, player, comment, QR and directory endpoints count side effects; only the full actual page methods decide when to call them. The UI constructor is not executed, and the fixture never reconstructs XAML or transplants a VM. The constructor is the original product source outside extracted scope; wrappers instantiate the same fields for the declared managed mechanism test.

Transport uses optional deterministic TaskCompletionSource gates and real-response JSON; normal stub defaults and historical tests remain unchanged. Synchronous controlled release keeps request accounting deterministic and does not claim multithreaded UWP stress. Decode is gated separately through the optional HttpResults decode hook. The modeled async-void synchronization boundary captures thrown callbacks only to count them: green requires zero such errors and one active-operation log; red fails that requirement. All gates/tasks settle, static transport hooks clear, and non-inlined creator/drain frames return before test-only GC/reachability assertions. Active/omitted-close controls prove roots can be detected. No app GC calls or hidden canceled request/weak-reference claims were added.

Native host evidence: Linux6.18.57 x86_64/glibc2.41; Python3.12.14; both powershell and pwsh absent (`environment.json`). No Windows VS/UWP compiler, XAML/COM/cache delivery, decoded playback, Windows process sampling, native decoder/display/SMTC/timer/GPU or actual RAM slope is verified. The source correctness race is repaired within this fixture; BR-21/native50 watched-video slope remains unmeasured/open. Root owns common stable integration only after every source owner freezes, independent implementation review, later single-owner main documentation and separately granted serialized publication. No commit or pushed SHA is claimed.
