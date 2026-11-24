# 审查：E1 / UX-02 comment submission recovery

- 轮次：`20261124-api-request-maintenance`
- 审查类型：需求
- 审查员：`ux_review`
- 结论：**PASS** — CR-R1 and CR-R2 closed on independent re-review, 2026-11-24.
- Bound source baseline: `41dc562fba6e45896b0c4ba604f9ec38f4661e73`; comment dialog remains unchanged during this review. Preceding-round implementation is active and is not reviewed or modified here.

## 审查范围与证据

Read this round's plan/review dispatch, all three final requirements/evidence pairs, repository/cloud instructions and memory index, the approved preceding logic/UI contracts, verification baseline and active execution dispatch. Inspected the real unchanged SendCommentDialog XAML/code-behind and caller; read the bound original action parser, HttpResults/legacy DTO and comments probe seam. Read the scratch project, complete Program/Stubs, source hashes and recorded red output. Only this review artifact is owned/written; no product, test, build, requirement, main-doc or prior review changes, no commit/push, no live write.

The revised synchronous primary adapter, exception-contained owned Task, typed action result, preserved raw text/target, busy/read-only state, retry and logical abandonment form an executable bounded design. Re-read revised requirements/evidence and the round plan's explicit decision to preserve existing decoder compatibility. Both original P1 findings below are closed; their history is retained. No new framework, credential change, global transport rewrite or fabricated device BR is needed. Requirements PASS still awaits explicit executor assignment and dependency/ownership release before execution.

### Official UWP API verification

Used `c0/read-docs` and Firecrawl to inspect the Windows.UI.Xaml UWP references, requesting `winrt-19041`:

- [Cancel](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.contentdialogbuttonclickeventargs.cancel?view=winrt-19041): “A true value for Cancel cancels the default behavior.”
- [GetDeferral](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.contentdialogbuttonclickeventargs.getdeferral?view=winrt-19041): provides a deferral for asynchronously responding to a button event.
- [ContentDialog example](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.contentdialog?view=winrt-19041): missing fields set Cancel=true; async work awaited inside the button handler obtains a deferral before awaiting and completes it afterward.
- [Closing](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.contentdialog.closing?view=winrt-19041): “Occurs after the dialog starts to close, but before it is closed and before the Closed event occurs.”
- [Closed](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.contentdialog.closed?view=winrt-19041): “Occurs after the dialog is closed.”

All retrieved pages returned status 200; property/event pages list builds 18362 and 19041. Learn also exposes current canonical/default monikers and sign-in page chrome; the relevant API descriptions were readable. This is source documentation, not a tested UWP runtime guarantee. Synchronously setting Cancel and then owning a separate Task is a reasonable design inference from these contracts; it must remain separate from an awaited button-event/deferral design.

## P0（必须修复，阻断）

None found in this requirement scope. U1 remains the separately approved request-construction dependency, not work granted by this review.

## P1（必须修复，可本轮闭环）

### CR-R1 — CLOSED: invalidate at dismissal start, not only after Closed

**Original finding:** the first reviewed requirements §3 (`requirements.md:39–47` in that version) invalidated secondary cancellation synchronously, but handled Escape/system/external dismissal through **Closed** only. Its acceptance required those dismissals to suppress later notices. Official UWP docs distinguish Closing from Closed; Closed is already after closure. A pending result delivered after external dismissal starts but before Closed could still be considered current, publish success/error, restore retry controls or call Hide again under that state table. Raising Closed immediately in a stub did not exercise that interval.

**Required bounded revision:** specify and wire an earliest accepted dismissal transition for Escape/system/external Hide, normally synchronous `ContentDialog.Closing` invalidation in these two owned dialog files, with Closed retained as idempotent cleanup/fallback. Define the race explicitly: an accepted close-start transition retires the operation before a later continuation can publish; success processed first marks terminal before its own Hide. Do not add a delayed/cancellable Closing deferral or a new reopen policy. Secondary cancel still invalidates before its explicit Hide. If a different hook is chosen, identify its UWP contract and how it covers all three dismissal routes before Closed.

**Verification required in revised acceptance:** deterministic real-code tests with separate Closing and Closed signals: begin send → begin external/Escape dismissal → complete success/rejection/fault **before Closed**; expect no publication, restoration, request or extra Hide. Then raise Closed twice and complete cleanup harmlessly. Cover success-first ordering and a fresh dialog B while A's pending result finishes. Keep corresponding Windows checks explicit because fake events cannot establish platform routing. Add the official Closing/Closed distinction to the evidence rather than describing Closed as the start of cancellation.

**Owner:** requirements owner revises only this stream's requirements/evidence; later explicit executor owns the existing dialog/probe scope. This finding grants no active implementation file ownership.

**Closure verified:** revised §3 now requires synchronous accepted **Closing** retirement, constructor/XAML event wiring, an idempotent shared retirement path, defensive Closed cleanup, no Closing await/deferral/cancellation and no dialog-instance reuse. Success-first marks terminal before Hide; Closing-first suppresses all later outcomes. §6 separately schedules success/rejection/fault between Closing and Closed, repeated Closed, success-first and fresh-B isolation. Busy face insertion explicitly guards callbacks from an already-open flyout. §7 retains real Windows routing/animation/focus checks. The official Closing/Closed distinction is now in evidence.

The additional exact-source baseline race experiment is appropriately red: 3 failures / 8 checks against unchanged dialog code, with independently raised events and no retirement logic in stubs. The five controls are explicitly not cleanup-implementation evidence. This resolves the missing contract without representing the source defect as already fixed.

### CR-R2 — CLOSED: reconcile actual raw-body compatibility and the proposed stub

**Original finding:** first-reviewed §4 required the existing action parser but said nonobject/malformed JSON or HTML never succeeds. The real parser calls `HttpResults.TryGetJObject` (`Parsing/Comment/WebCommentActionParser.cs:39–43` at the bound baseline). Production `Helpers/HttpHelper.cs:580–599` first attempts JObject.Parse, then uses `FindLongestValidJsonString` (`:509–551`) to salvage an embedded object. With successful transport, an HTML/prefixed body such as `<html>{"code":0}</html>` can yield a valid zero-code object under that policy. PB1 only changes integer-code validation, not this body policy.

The existing comments HttpResults fixture uses only strict `JObject.Parse` and lacks production's salvage fallback. Reusing it as the sole evidence (`requirements.md:72`) could make “HTML/malformed never succeeds” green while the real response path still accepts an embedded success object. Calling the stub non-interpreting does not remove that difference.

**Required bounded revision:** make the raw-body compatibility policy explicit and reconcile it with the stated parser/no-shared-helper scope. Choose and justify either preserving the existing typed parser's salvage behavior (with accurately limited claims about truly unparseable responses), or enforcing a scoped strict document boundary with an explicitly owned, reviewed implementation shape. Do not silently loosen assertions, change global HttpHelper/parsers, or make the test double enforce strictness that production lacks. The ordinary missing-code, failed-transport and noninteger-code false-success protections remain mandatory in either design.

**Verification required in revised acceptance:** use actual HttpResults decoding methods through a faithful exact-source extraction/link, or an equivalently direct product seam, for raw-body compatibility evidence. Include plain HTML without JSON, HTML/prefix plus `{"code":0}`, malformed wrappers with an embedded object, and concatenated objects; state expected outcomes for the selected policy. Retain real parser inputs and UI decisions, declared platform substitutes and source hashes. The narrow existing fixture may still cover dialog state mechanics if its limitations are named; it cannot certify a different production body policy. No live HTTP is needed.

This was a source-verified contract discrepancy, not a newly reproduced device defect. At the initial review the embedded-object counterexample was reasoned from the inspected method bodies; the re-review additionally inspected and ran the supplied exact-source compatibility binary, as recorded below. No scratch harness was changed by this reviewer.

**Owner:** requirements owner supplies the bounded policy/test revision, followed by independent re-review before implementation.

**Closure verified:** the round plan explicitly selects existing decoder compatibility. Revised §4 withdraws the blanket HTML/malformed rejection claim, describes whole-object parse plus the actual longest-candidate depth scan and equal-length first-candidate behavior, and limits recoverability claims to the stated fixtures. It keeps selected-object missing/noninteger/out-of-range codes and failed transport as failures. There is no second decoder, shared-helper edit or permissive default-zero fallback.

Revised §5 requires exact production HttpResults class-body extraction under ignored/temporary output, only namespace/usings wrapping, real legacy model dependency and pinned Newtonsoft.Json 13.0.3. The real dialog and parser must execute against that class in the permanent raw-response run, with no duplicate HttpResults type. The old strict fixture can cover mechanics only and its salvage omission is disclosed. Narrow `comments/run.sh` ownership is proposed solely for that portable extraction/run seam. Generation errors must fail visibly; no rewritten method, copied decoding algorithm, committed generated product copy or test-only success policy is allowed.

The 15/15 raw-body compatibility experiment uses actual extracted decoding and a bound real parser; it is explicitly parser/decoder supporting evidence, not post-PB1 validation or E1 dialog green. §6 requires permanent red/green at the exact post-U1/PB1/pre-E1 SHA and includes actual dialog decisions over the raw-body fixtures. Ordinary false-success, stale-generation and draft/retry protections remain mandatory. This resolves the contract/test mismatch without weakening those protections.

**Unresolved P0/P1 requirements findings: none in this scope.**

## P2（记录到 ROADMAP）

No additional P2 work is needed to obtain requirements PASS. Preserve the existing UI follow-up boundaries: no layout/theme redesign, no draft persistence across intentional dismissal, no root/nested-reply refactor, and no automatic resend. Busy/error/focus accessibility in this touched dialog remains required and Windows-blocked, rather than being converted into a global UI audit.

## 验收标准核对

| Contract | Review disposition |
| --- | --- |
| Synchronous primary adapter | Adequate: Cancel=true before validation/dispatch, no await in the adapter, one exception-contained Task. A switch to awaited event/deferral requires revised lifetime/cancellation coverage. |
| Blank and duplicate activation | Adequate: blank/null/whitespace sends nothing and stays editable; establish guard before first await; a forced second callback sends nothing regardless of button-disabled behavior. |
| Sending/draft snapshot | Adequate: capture raw text/oid/type once, preserve leading/trailing spaces/Unicode, lock editing and face insertion, show busy state, keep cancellation usable. “Disable face insertion” also applies to callbacks from an already-open face flyout; disabling its opener alone cannot establish the invariant. |
| Typed success and failure | PASS: transport must succeed and the object selected by real HttpResults decoding must carry valid Int32 zero; no data payload is required; missing/bad codes never become default zero. PB1 remains a hard dependency. Existing salvage semantics and exact raw-body fixtures are explicit; CR-R2 closed. |
| Retry/uncertainty | Adequate: current failure retains exact draft, restores Ready/send/face/editing and one readable error. Unknown delivery warns to refresh before manual retry; no exactly-once/server-rollback or “cancel unsent it” claim. |
| Cancel/late completion | PASS: synchronous secondary/Closing retirement, Closed fallback, explicit first-transition ordering and independent close-start/close-finish tests address CR-R1. Every Task remains observed/contained even after abandonment; fresh-instance status must remain isolated. |
| Tests | PASS: full real dialog code-behind, real CommentApi/parser/DTO, actual response decoding for compatibility, deterministic delivery and real state decisions are required. Existing comments/U1/PB1 assertions remain intact. Expanded permanent red/green at the post-dependency SHA is required; original 8/10 red, 3/8 Closing red and 15/15 decoder compatibility are distinct supporting evidence. |
| Platform | Honest: Linux can verify linked-source state/response logic but not WinRT rendering, real event routing, focus, keyboard or XAML compilation. No device report or live write is invented. |

### Ownership and cross-stream consistency

- Execute only after this requirements review passes, U1/PB1 finish and their shared comments ownership is explicitly released. Do not rebuild or reinterpret an intermediate parser/builder as the stable pre-change baseline.
- Product scope remains `src/BiliLite.UWP/Dialogs/SendCommentDialog.xaml.cs` and `.xaml`; CR-R2 preserves production decoder/parser semantics and grants no shared product file.
- Existing `comments/CommentsProbe.csproj`, `Program.cs`, `CommentProbeStubs.cs`, narrow `run.sh` exact-source extraction/run work and explicitly assigned small dialog/raw-response harness sources have one later executor. Preserve original and newly added U1/PB1 assertions; do not overwrite an in-flight runner/project/stub file. All runner changes remain LF/Bash, surface failures, preserve normal batch execution and introduce no global runner/dependency migration.
- Favorite U9 uses FavoriteAPI/c2 and search U5/U8 uses SearchAPI/api02 with its shared-stub release dependency. These streams do not justify changes to the dialog's CSRF, encoding, transport or credentials.
- PLAN/ROADMAP/VALIDATION updates remain with the single later documentation owner. Record the E1 source diagnosis; create a BR only for an actual reported defect under the established policy.

## 测试/构建证据

The unchanged dialog's SHA-256 is `4cc635221899d3a8e537af55306ed9f8ee15c11d723ea85dab5fa2a124c83871`, matching supplied evidence. Current HEAD at this inspection was `5eec54ecd094bdb51ec43b42d9f901d8fae7c993`; preceding implementation is active, so this does not relabel all baseline source inputs with that SHA. `git status --short` for the two dialog files was empty. `git diff --check` exited 0.

Inspected scratch project inputs and the exact legacy HttpResults/ApiModel extraction, UI/dependency stubs and synchronization-context pump. Independently executed the **already-built baseline binary**, avoiding a rebuild against actively changing shared sources:

```text
DOTNET_CLI_TELEMETRY_OPTOUT=1 /workspace/work/maintenance-foundation/quality/dotnet/dotnet \
  /workspace/work/maintenance-foundation/comment-recovery/bin/Debug/net8.0/DialogRecoveryScratch.dll
exit=1
CHECKS=10 FAILURES=8; actual dialog/builder/HttpResults/DTO source; UI+transport fakes
```

The eight named failures matched the artifact: missing-code success, failed-transport zero success, disabled rejection retry, uncancelled rejection/blank/delayed primary, overlapping forced callbacks and late success after explicit cancel. Two controls preserve the exact rejection draft and send no blank request. This confirms that supplied old binary's output and basic test feasibility; it is not a new compile, a post-U1/PB1 baseline, coverage of Closing→Closed, a strict-body-policy proof or a product fix.

Required eventual commands: owning comments batch with Bash/the declared scratch .NET 8 environment and `git diff --check`; on Windows, `powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump` plus `-Restore` when required. Windows UWP compile and real dialog/keyboard/focus/close routing remain **FAILED/BLOCKED — Windows/VS/UWP SDK unavailable**. An offline result must not be described as fresh device acceptance or release readiness. This reviewer did not start the app, build UWP, or perform any authenticated/live action.

### Independent re-review evidence

Inspected the new ClosingRaces and RawBodies project/source inputs, independent event stubs, manifests and pinned-dependency references. A read-only class-text comparison confirmed the entire current production HttpResults class body appears **verbatim** in `HttpResults.extracted.cs`; current HttpHelper hash remains `b12e457a75a133e9298fb77ad982b33986c2f95da2562b4f031b1fd692424d13`. Frozen and current dialog hashes both remain `4cc635221899d3a8e537af55306ed9f8ee15c11d723ea85dab5fa2a124c83871`. The compatibility program's dependency substitutes are inline in Program.cs; there is no separate Stubs.cs required by that project.

Executed only the two already-built supplied baseline binaries with the existing scratch .NET host, without rebuilding against active preceding-round work:

```text
dotnet /workspace/work/maintenance-foundation/comment-recovery-closing-races/bin/Debug/net8.0/ClosingRaces.dll
exit=1; CLOSING_RACE_CHECKS=8 FAILURES=3

dotnet /workspace/work/maintenance-foundation/comment-recovery-body-compatibility/bin/Debug/net8.0/RawBodies.dll
exit=0; RAW_BODY_FIXTURES=15 FAILURES=0
```

Results exactly matched revised evidence, including embedded/prefixed-zero success, longest/equal-length selection, plain-HTML/unfinished/array failures, and Transport prevailing over an embedded zero. The Closing experiment's fresh-B control checks its controls/draft only; permanent acceptance correctly demands stronger status/global-notice isolation as well. These results establish old-source behavior and proposed test feasibility, not a recovery implementation, new source compile or Windows outcome. Re-review HEAD observation was `858b97105ac8b46492af3810e5352afd9efbe8d5`; frozen inputs retain their original source identities. `git diff --check` exited 0.

**Final requirements disposition: PASS.** CR-R1/CR-R2 are closed, with no product scope expansion. Explicit executor assignment, U1/PB1 completion and ownership release still precede execution. Implementation review must verify unchanged permanent red/green assertions, real decoder/dialog fidelity and the explicit **Windows build/device/session BLOCKED** limits; this PASS claims none of those platform outcomes.
