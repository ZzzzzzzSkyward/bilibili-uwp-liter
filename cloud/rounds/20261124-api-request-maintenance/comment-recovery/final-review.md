# 审查：E1 / UX-02 comment recovery implementation

- 轮次：`20261124-api-request-maintenance`
- 审查类型：实现
- 审查员：`ux_review`
- 结论：**PASS — implemented and independently offline verified; Windows build/device/session gates remain BLOCKED.**
- Immutable source/test commit: `1c7f6e4aacf3c0fa4bb7f7d70851308405b995cd`.
- Pre-change dialog source: parent `7ff548b24dc70a335c6d0d44c4ebc86a984ce96c`.

## 审查范围与证据

Read repository/cloud instructions, memory index, passed revised requirements/review, the decoder-compatibility decision in the round plan, execution/implementation-review dispatches and execution evidence. Reviewed the immutable commit's complete product/test/runner diff, whole dialog and new probe/stub sources, supplied red/green logs and real response-decoder boundary. Later branch commits and other owners' uncommitted artifacts were preserved; they do not replace the reviewed SHA.

Only this `final-review.md` is written in the repository. Independent verification used isolated scratch snapshots under `/workspace/work/maintenance-foundation/ux-e1-review/`; no product/test/main-doc/history changes, shared generated-output races, application launch or live comment writes occurred.

## P0（必须修复，阻断）

None found in the reviewed implementation.

## P1（必须修复，可本轮闭环）

None found in the reviewed implementation. Requirements findings CR-R1 and CR-R2 are implemented and independently verified within the approved scope.

## P2（记录到 ROADMAP）

No new code/structure issue requires a P2 disposition in this slice. The specified Windows XAML/WinRT/focus/accessibility/session observations remain explicit platform blockers, not inferred passes or newly invented device defects. Existing unrelated UI backlog remains outside this approval.

## 验收标准核对

| Requirement / implementation boundary | Finding |
| --- | --- |
| Synchronous primary cancellation | PASS. `SendCommentDialog.ContentDialog_PrimaryButtonClick` sets `args.Cancel=true` before validation/guard/dispatch and contains no await. Primary default closure is not delegated to a deferred button event. Existing XAML wires primary/secondary to the actual methods; no mixed deferral/default-close policy is introduced. |
| Single request and owned completion | PASS. `_isSending` and generation are established before the first await; the held `_sendTask` executes `SendCommentAsync`, whose expected request/parser/fault paths are exception-contained. Repeated callbacks while pending or retired cannot dispatch. The test inspects a real pending Task and completion, rather than treating a disabled button as proof of single flight. |
| Draft and busy interaction | PASS. Exact nonblank raw text is captured once; readonly oid/type and real AddComment preserve target/encoding ownership. Pending state disables send/edit/face opener, displays progress and `正在发送`, and guards callbacks from an already-open face flyout. Blank/null/whitespace keeps the draft editable, displays inline validation and sends nothing. |
| Typed success | PASS. The dialog calls the existing `WebCommentActionParser`, after an ownership check. Successful transport and valid selected-object Int32 zero are required; payload data is optional. Missing/bad/out-of-range codes and failed transport cannot reach the success path. The legacy GetData/default-zero decode is removed from this dialog. |
| Retry and uncertainty | PASS. Rejected responses show the useful typed message; other unsuccessful responses/faults show the explicit unconfirmed-delivery/refresh-before-retry text. `RestoreReady` checks ownership, clears sending/busy/read-only state, retains the exact draft and restores a usable editing focus target. There is no automatic retransmission, duplicate failure toast or claim of server rollback. |
| Success retirement | PASS. Accepted success calls `Retire` before one success notice and explicit Hide. A reentrant later button/Closing/Closed signal cannot send/publish again. Failure never calls Hide. |
| CR-R1 early dismissal | PASS. Constructor wires real Closing and Closed events. Secondary synchronously retires before one explicit Hide and cancels default handling. Closing immediately retires without awaiting/cancelling/deferring the close; Closed is idempotent defensive fallback. A continuation between Closing and Closed cannot publish success/error, restore UI, dispatch or Hide. |
| Cross-instance isolation | PASS. Retirement/generation/task state belongs to each dialog. A's late completion in its closing interval cannot modify B's draft/status/busy state or publish a stale global notice. Logical abandonment observes/contains later completion without adding shared transport cancellation. |
| CR-R2 actual decoding | PASS. Global HttpHelper/parsers are unchanged by this commit. The probe compiles the exact extracted production HttpResults class, real parser and real dialog. Embedded/prefixed zero may succeed under the preserved salvage policy only for a current successful-transport operation. Plain HTML/unusable fixtures, selected-object missing/noninteger code and failed transport remain failures. Longest-object/first-equal-length behavior is exercised honestly; no universal malformed-document rejection is claimed. |
| Error and face-event containment | PASS for the specified paths. Synchronous/delayed transport faults restore current retry or remain silent after retirement. Face service errors are caught. New logs use category-only messages, without raw draft/body/response/exception details; user text remains short Chinese with explanations in the status region. |
| XAML and user feedback | Source wiring is correct and XML is well formed. The added status/progress controls use a shared spacing resource, theme brush and polite live-region property; send/cancel labels are retained. Actual UWP compilation, rendered layout, keyboard focus and Narrator announcement remain unverified on this Linux host. |

The task's scope is preserved: product changes are confined to the two SendCommentDialog files. No API/CSRF builder, parser, cookie/credential/transport, setting, dependency, manifest, asset or live JSON behavior is changed. The favorites execution-artifact addition in this commit only records the same sequential executor's preceding U9 SHA/remote evidence; it changes no other product behavior or test expectation.

## 测试/构建证据

### Independent immutable red/green reproduction

Built two isolated snapshots from git object contents, each with the exact E1 permanent probe/runner/dependency/source inputs. The red snapshot substitutes only the **two original dialog files from parent `7ff548b`**; every test input is byte-identical between red and green. The green snapshot uses the immutable `1c7f6e4` inputs. Snapshot source hashes are recorded in scratch `source-manifest.json`; no branch checkout or shared source file was rewritten.

The initial minimal snapshot omitted the runner's read-only HttpHelper input; extraction failed before compilation. Added that unchanged commit-bound input to both snapshots. That setup failure is not counted as product red evidence. The authoritative red/green below both compiled successfully.

Ran `bash plan/tests/comments/run.sh` from each snapshot, using the existing SDK 8.0.427/runtime 8.0.33, pinned NuGet Newtonsoft.Json 13.0.3, isolated CLI homes and the installed package cache:

| Snapshot | Actual batch exit | Result |
| --- | --- | --- |
| Original dialog + final permanent tests | **1** | `DIALOG_CHECKS=153 FAILURES=109`; existing parser/U1/PB1 checks executed before these failures. |
| Corrected immutable E1 commit | **0** | `DIALOG_CHECKS=153 FAILURES=0`; **PASS 1070 assertions**. |

The independent results match the executor's recorded red/green. The original 917 checks are retained: Program changes add the dialog test invocation/count only, and the existing assertions are unchanged. All 153 new checks execute real dialog/builder/parser/decoding code. This is not source-string-only lifetime coverage or a stubbed recovery implementation.

Coverage includes exact Unicode/multiline/space/high-ID/type snapshots; blank inputs; delayed rejection and explicit retry; duplicate activation; already-open face callbacks; null/invalid/rejected/transport/code-boundary results; synchronous and delayed faults; all specified raw-body compatibility cases; secondary/Closing/Closed-fallback × success/rejection/fault; continuation before Closed; repeated cleanup; success-first ordering; fresh-B status/busy/global-notice isolation; and escaped async-event exceptions.

Independent logs remain in scratch `red.log` and `green.log`. Both runs have the same existing **CS1998** warning from actual HttpResults.GetJson; no warning is suppressed and no compiler error occurred. No whole-suite or UWP-build claim follows from this affected batch.

### Extraction and test-double fidelity

Reviewed `comments/run.sh`: Bash/awk copies from the exact HttpResults declaration through the final namespace brace into ignored `obj/probe-generated/HttpResults.cs`, prepending only usings/namespace opener. Missing boundary/input fails before dotnet executes; no stale generated success or silent fallback decoder is used. The regex accommodates CRLF, and no Python/runtime dependency or generated source copy is committed.

Byte comparison in **both** independent snapshots confirmed that the complete original product class/namespace suffix is exact. Hashes match executor evidence:

| Input | SHA-256 |
| --- | --- |
| Unchanged HttpHelper.cs | `b12e457a75a133e9298fb77ad982b33986c2f95da2562b4f031b1fd692424d13` |
| Generated wrapper, red and green | `8aa4c8734aed85517df1dc3863a479fc076a262e775a7c657e45e9f8b9bc6a93` |
| Corrected immutable dialog | `71cc068c7e38fc70eb12d9d8e4cc087b4f43a5fc2eafe47a451bd102524ccde6` |

The prior strict-only HttpResults fake is removed. Platform stubs expose properties/generated fields and independently raisable Closing/Closed; Hide only records. They do not retire operations, decide response acceptance or synthesize framework routing. The deterministic synchronization-context pump and controlled TaskCompletionSource delivery exercise actual continuations without sleeps/live networking. The source check of XAML event names complements reflection-driven handler calls; it is not represented as rendered event-routing validation.

`git diff --check 1c7f6e4^ 1c7f6e4` exited 0. Parsing the immutable edited XAML as XML succeeded; this confirms syntax only.

### Platform limits and required follow-up

Windows compile remains **BLOCKED**, with the recorded exact prerequisite failure `powershell: command not found` / exit 127 on Linux. Required command on the documented Windows/VS/UWP SDK host:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump -Restore
```

Windows follow-up remains the requirements' controlled delayed send/rejection/blank/cancel/success flow, real Escape/system/external Hide reaching Closing before Closed, completion during the closing interval, already-open face protection, usable keyboard retry, readable themed status and actual Narrator/live-region behavior. Console stubs cannot establish those platform observations, installed launch, server delivery or authenticated-session validity. No real comment was written or new device BR invented, and no release/runtime-ready approval is given.

## Source and file structure conclusion

**PASS within the touched scope.** Send state/ownership/retirement helpers stay local to the dialog; request construction, action parsing and raw decoding retain their existing owners. No duplicate production parser/state framework, orphan production helper or unrelated cleanup is introduced. New harness files belong to the existing comments batch, and the generated response wrapper is an ignored build input rather than a second maintained implementation. Existing assertions and source boundaries remain intact. No unresolved P0/P1/P2 code or structure finding was identified for this slice; platform blockers remain explicit and separate.
