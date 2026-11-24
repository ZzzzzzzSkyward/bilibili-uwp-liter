# UX-02 / E1 — comment send lifetime and retry

Status: **REVISED — READY FOR INDEPENDENT RE-REVIEW; P1 recommended.** CR-R1/CR-R2 addressed below per review and orchestrator decision; execution still waits for PASS, U1/PB1 completion and shared comments-probe ownership release. No device bug report is invented.

Baseline: `dev`, `41dc562fba6e45896b0c4ba604f9ec38f4661e73`, 2026-11-24. Read round plan; AGENTS/CONTRACT/REPO; memory INDEX; prior logic/UI requirements and verification baseline. Product path shorthand below is under `src/BiliLite.UWP/`. [Evidence](evidence.md) records official UWP references, real-source scratch failures and Linux limits.

## 1. Current behavior and evidence class

| Finding | Current evidence | Classification |
| --- | --- | --- |
| Button lifetime unmanaged | `Dialogs/SendCommentDialog.xaml.cs:28–34,39` awaits in async-void primary event without Cancel/deferral; blank return also leaves default close enabled. | Source-confirmed omission; actual visual closing/focus not device-reproduced. |
| False success | `:40–44` uses legacy `GetData<object>()`; `Models/ApiModel.cs:5` defaults missing code to zero. `Helpers/HttpHelper.cs:553–574,619–628` deserializes body independently of status. | Actual-source scratch reproduced success toast/Hide for `{}` and `status=false, {"code":0}`. |
| Cannot retry a rejection | Primary disabled at `:37`, restored only in catch (`:55`); business rejection at `:49` does not restore it. | Actual-source scratch reproduced disabled retry after nonzero code. |
| Duplicate/late publication possible | No in-flight guard or closed generation; secondary calls Hide only (`:61–64`). | Actual-source scratch reproduced two overlapping callbacks and success toast/second Hide after cancel. A forced second callback tests ownership, not actual OS click routing. |
| Suitable typed parser exists | `Parsing/Comment/WebCommentActionParser.cs:24–73` separates Transport/Envelope/Rejected and accepts integer zero without requiring data; CommentControl uses it at `:383,524,570,631`. | Existing contract; PB1 must first harden out-of-range code handling. |

Original scratch: 8 failures / 10 checks. Revision evidence adds 3 failures / 8 separate Closing/Closed race checks against the unchanged baseline dialog, plus 15/15 existing raw-body compatibility fixtures passing against actual HttpResults decoding and typed parser. These are separate suites, not a recovery fix or post-U1/PB1 baseline; details/hashes in evidence. No production behavior was supplied by a new implementation.

## 2. Goal and non-goals

Keep a comment draft editable/retryable after unsuccessful sends, suppress duplicate in-flight requests, and close/report success only for accepted current-session responses. Preserve exact raw text and comment target/type.

No comment API/CSRF repair here (U1 owns it), parser rewrite (PB1 owns it), global helper/transport change, automatic retransmission, authentication flow change, count refresh, root/nested-reply redesign, draft persistence across intentional dismissal, new app service/framework or theme/accessibility redesign. `CommentControl.xaml.cs:671–674` constructs a new dialog per open; reuse of a closed dialog instance is outside this slice.

## 3. Exact event and state contract

Choose a **synchronous primary event adapter**: set `args.Cancel=true` before validation or dispatch, then start one exception-contained `Task` operation. The event itself must not await. This cancels platform default closing immediately, keeps cancellation usable, and makes explicit `Hide` on accepted success the only send-triggered close.

Official UWP docs say Cancel=true cancels default behavior and async work *inside a button event* requires a deferral before await; see evidence. Moving the awaited work into an owned Task avoids holding a button-event deferral across a cancellable network request. Do not mix “default close on completed deferral” and explicit Hide paths. If review selects an awaited event instead, requirements must be revised with exact cancel-during-deferral behavior and exactly-once finally completion before execution.

| State / trigger | Required transition and UI | Request/publication rule |
| --- | --- | --- |
| Ready + blank/null/whitespace | Stay Ready; keep open; visible concise validation text; draft unchanged; enabled send. | Zero requests, no success/Hide. Test null defensively through stub; actual TextBox normally yields a string. |
| Ready + nonblank send | Capture raw text/oid/type once; establish active generation and in-flight guard **before first await**; enter Sending. | Exactly one AddComment request using captured values. Do not trim/change nonblank text sent. |
| Sending + second primary callback | Stay Sending and keep default close cancelled; retain existing pending operation. | Zero additional requests, no queued retry. Guard in code, not only disabled button. |
| Sending UI | Disable primary; make draft read-only; disable face opener **and guard callbacks from an already-open face flyout** so pending send cannot alter text; show `正在发送`/indeterminate busy indicator. | Secondary cancel stays usable; no timer loop/automatic retry. |
| Sending + accepted current success | Mark terminal before UI publication; show one success notice and close once; no send restoration that could admit a second request before Closed. | Success means typed action result IsSuccess, transport successful and integer code=0; data optional. |
| Sending + current rejection/invalid response/exception | Return Ready; retain exact draft; clear busy/read-only state; restore face/send; show one readable error and allow explicit retry. | Never success or Hide; no duplicate toast+inline copies of same error. Exception must not escape async event/Task. |
| Ready/Sending + secondary cancel | Invalidate current generation/mark closed synchronously before Hide; dismiss once. | No automatic resend, no new attempt from that instance. Pending transport may finish; it no longer owns UI. |
| Any nonterminal state + **Closing** (Escape/system/external Hide) | Synchronously retire active generation at accepted dismissal **start**; do not await, defer or cancel Closing. Dialog has no other closing-cancellation policy. | Continuation arriving after Closing but before Closed has no UI/state ownership: no notice, restore, send or extra Hide. |
| Terminal + Closing/Closed; or fallback Closed | Idempotent terminal cleanup; Closed also invalidates defensively if Closing was not delivered. | Observe pending Task completion without publication; do not replace Closing with Closed as the first dismissal hook. |
| Retired/Closed + primary/late completion | Remain terminal; leave draft/state alone. | No request, no status mutation, no toast, no extra Hide. New opening uses a new dialog instance as existing caller does. |

Wire Closing and Closed on this owned dialog (constructor subscription or XAML events), with a single idempotent retirement path also used before secondary Hide. [UWP Closing](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.contentdialog.closing?view=winrt-19041) occurs after closing starts but before Closed; [Closed](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.contentdialog.closed?view=winrt-19041) occurs after closure. No new cancellable/delayed Closing or reopen contract is introduced.

The code runs on the owning UI thread; no new cross-thread locking is required. Test separate Closing and Closed signals with a pending continuation **between** them, plus a fresh dialog for isolation. A late operation may be logged safely, but never include raw draft, body, cookie/token or response text in new logs.

Cancellation is **logical abandonment**, not proof of network abort or server rollback: current `Utils.Request` has no cancellation token parameter (`Helpers/Utils.cs:35–48`). Do not add shared transport cancellation. A server may have accepted the post; never say cancel “unsent” it. Race order is explicit: accepted Closing first retires the operation before any later continuation; accepted success first marks terminal before its own Hide and subsequent Closing/Closed cannot duplicate publication.

Successful Hide and explicit cancellation invalidate before any reentrant Closing/Closed handler runs. Current failure may restore Ready only while it still owns the active generation. Abandoned cleanup can release private resources but cannot restore controls or clear another dialog's status.

## 4. Request, response and error contract

- Preserve constructor/API surface, target oid string precision, enum type and `AddComment` raw-text calling convention. U1's builder owns one-time encoding/CSRF. No extra encoding, decode or credential mutation in the dialog.
- Replace legacy GetData with existing `WebCommentActionParser.Parse(HttpResults)`; do not use generic ApiEnvelope.Read, which requires a data/result payload. No legacy optimistic default `code==0` fallback.
- Accept successful transport plus a decoded action object with integer code zero; data remains optional. Nonzero integer code is Rejected with useful message/fallback; null response and status=false are Transport even if embedded body has zero code.
- **Preserve actual HttpResults raw-body compatibility**, per orchestrator decision: TryGetJObject first tries whole JObject.Parse, then FindLongestValidJsonString. The fallback scans brace/bracket depth, parses candidate substrings as objects, retains the longest parseable object and keeps the first on equal lengths. It is not a strict document boundary or a JSON-string-aware general salvage guarantee; do not rewrite this policy here.
- Missing/null/string/boolean/floating/array/object/out-of-range code in the **selected object** never succeeds. Bodies with no decoded object fail Envelope; plain HTML without JSON fails, but HTML/prefix/malformed wrappers containing a salvageable zero-code object can succeed. PB1 supplies typed out-of-range failure. Do not claim all HTML/malformed/concatenated bodies are rejected or add a local second decoder.
- Business rejection keeps draft and offers retry; transport/unknown completion explains uncertainty, e.g. `发送结果未确认，请先刷新评论再重试。` No blind automatic retry: exactly-once server delivery is not achievable from this API.
- Unexpected exception shows concise retry/uncertainty text and logs category/exception safely under existing logging conventions; no raw exception stack/body as dialog text. Parser remains UI-free; current operation alone chooses presentation.
- Error/busy state must be perceivable while dialog remains open; use a small status/error region and indeterminate progress. Keep labels `发送`/`取消`; explanations belong in status/help. No new persistent settings or draft files.

Use existing theme resources for new status/progress content. Keep the text box and buttons reachable without pointer; after failure, return a usable editing/send focus target without forcing a new post. These are targeted Windows checks for the touched dialog, not a requirement to retest all accepted comments UI.

Raw-body compatibility fixtures below define actual policy, not relaxed false-success rules. Required missing-code, noninteger-code, failed-transport and stale-generation protections remain unchanged. The previous blanket “HTML/malformed never succeeds” statement was incorrect for the real product and is explicitly withdrawn; the existing strict fixture cannot establish it.

| Raw body fixture (transport true unless stated) | Actual decoded object / required typed outcome |
| --- | --- |
| `<html>error</html>`; `{"code":` | No object / Envelope failure; no success or close. |
| `<html>{"code":0}</html>`; `prefix {"code":0} suffix` | Salvaged code zero / success only if operation is current. |
| `{"bad":} prefix {"code":0}` | Invalid first candidate skipped, later zero object selected / success if current. |
| `{"bad": {"code":0}`; `[{"code":0}]` | No usable whole/candidate object / Envelope failure for these exact fixtures. |
| `{"code":0}{"code":-400,"message":"long rejection"}` and reverse order | Longer rejection object selected / Rejected -400 in either order. |
| `{"code":0}{"code":1}`; reversed equal-length pair | First object wins / success for first-zero pair; Rejected 1 when first is one. |
| `<html>{}</html>`; `prefix {"code":"0"}` | Decoded object lacks usable integer code / Envelope failure. |
| status=false plus `<html>{"code":0}</html>` | Decoder can find zero, but parser Transport must prevail / no success. |

## 5. Minimal change and test ownership after PASS

| Surface | Single executor ownership / limit |
| --- | --- |
| Product | `Dialogs/SendCommentDialog.xaml.cs`, `.xaml` only: synchronous adapter, owned Task/state, **Closing** retirement/Closed cleanup, busy/error/readonly UI and already-open face callback guard. No shared parser/transport edit. |
| Existing probe | `plan/tests/comments/CommentsProbe.csproj`, `Program.cs`, `CommentProbeStubs.cs`, `run.sh` and small dialog/raw-response harness sources in same batch. Runner ownership extends only to exact-source extraction for CR-R2, remains LF/Bash. Wait for U1/PB1 release; preserve all existing assertions. |
| Probe source inputs | Link actual dialog code-behind and existing parser/DTO, retain U1 real CommentApi link. Fake platform XAML controls/generated fields, emote service and HTTP delivery only; never fake parser success/recovery rules. |
| Docs/evidence | Assigned docs owner records source-confirmed E1 in PLAN/ROADMAP and targeted VALIDATION note. Do not invent a maintainer/device BR; actual reported regression follows existing BR policy. Executor writes stream execution.md with exact SHA/commands/results. |

Link full real dialog code-behind with partial generated-field stubs. Stub platform event args, **independent Closing/Closed signals**, Hide recording, properties and delivery, but no lifecycle decisions. Separate close-start and close-finish events; never raise Closed immediately as a substitute for the interval. Existing mapper assertions remain intact.

CR-R2 requires **actual HttpResults methods**, not the existing strict fixture, for raw-body evidence. Generate an exact class-body wrapper from read-only `Helpers/HttpHelper.cs` under temporary/ignored probe output; add only namespace/usings and actual `Models/ApiModel.cs` dependency, no method/body rewriting. The reviewed runner/project seam must compile real parser+dialog against that class (replace the fixture declaration for that run, avoid duplicate types), with UI/transport substitutes only. Keep generation portable under the existing Bash/.NET toolchain and surface generation errors; no new product helper/dependency or committed generated copy.

A separate mechanics run may retain the strict HttpResults fixture, but label its omission of salvage and never use it to certify raw-body compatibility. The permanent raw-response run must also exercise actual dialog decisions before/after recovery changes; a parser-only compatibility pass is supporting evidence, not dialog green acceptance. Record generated-source hash plus real source SHA/path and use pinned Newtonsoft.Json 13.0.3.

## 6. Behavioral acceptance and red-before/green-after evidence

Use controllable TaskCompletionSource responses and a deterministic synchronization-context pump or awaited real Task. No sleep/time-sensitive race tests, live HTTP, credentials, serialized source-string-only assertions, or detached task exceptions. Repeated callback counts reflect transport attempts; do not assume disabled controls prove single flight.

| Test group | Required assertions |
| --- | --- |
| Blank validation | Empty/whitespace/linebreak-only/null sends zero, cancels default close, stays open, draft unchanged and send usable. |
| Delayed rejection then retry | Before completion: busy, exact draft read-only, primary/face disabled and args.Cancel already true. Second callback adds no request; already-open flyout item callback cannot change text. Rejection restores Ready; explicit retry captures once and success closes once. |
| Response truth | Real parser: zero with/without/null data; -400 with/without message; `{}`, string `"0"`, null/bool/float/object/array code, integer boundaries+outside; status=false with zero body; null response. Raw-response run uses §4 table through actual HttpResults and actual dialog. Only current typed success notices/closes; salvage-compatible success is intentional. |
| Transport/Task exceptions | Throw before await and fault after delay; no unhandled exception, retained draft, busy cleared and send retryable. Rejected result is distinct from uncertain network outcome. |
| Text and target | Multiline `中文 & + = % 😀` and leading/trailing spaces remain byte-for-byte in raw input snapshot; target oid above 2^53 and original type unchanged. U1 separately checks actual encoded form. No extra request after success. |
| Abandonment | Secondary cancel then delayed success/rejection/fault: no late publication. Separately begin send → raise **Closing** for external/Escape dismissal → deliver success/rejection/fault **before Closed**: no toast/status/restore/request/extra Hide. Raise Closed twice afterward; cleanup harmless. |
| Race order/isolation | Success processed first marks terminal then Hide/Closing/Closed yields one notice/close; Closing processed first suppresses all later outcomes. While A is in Closing→Closed interval, fresh dialog B retains its draft/status/busy state when A completes. No new reopen policy. |
| Lifetime/cleanup | Default close always cancelled on primary paths; explicit successful Hide once; failure stays open. Every launched Task is observed/exception-contained. If a deferral is approved instead, acquired before await and completed exactly once in finally for every outcome must additionally be tested. |

Original 8/10 red plus revision Closing race 3/8 red and raw-body 15/15 compatibility pass are hash-bound supporting evidence only. Executor must rerun expanded permanent red at exact post-U1/PB1/pre-E1 SHA, including Closing→Closed and actual raw-body/dialog paths; then change product and require green. Compatibility pass proves preserved behavior, not a recovery fix; do not relabel old binaries as new baseline.

Run `UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality bash plan/tests/comments/run.sh` using the existing local .NET 8 SDK when available; use stream-owned output/cache or an immutable snapshot if ownership remains shared. Preserve existing comments/U1/PB1 assertions and record total count, exit, warnings and SHA. No whole-suite pass follows from this batch.

## 7. Platform gates, risks and terminal status

Linux can implement/verify linked-source state, raw decoding and event decisions. It cannot compile WinRT XAML or establish actual ContentDialog Closing/Escape/Closed/focus routing. Windows compile: `powershell -NoProfile -ExecutionPolicy Bypass -File .\build-release-x64.ps1 -Fast -NoVersionBump`; no platform SDK/PowerShell here, so **UWP BUILD + DEVICE BLOCKED**, never PASS.

Windows: delay send 5 seconds; verify visibility/busy/cancel; reject then retry unchanged draft; blank stays open. Observe Escape/system/external Hide reaching Closing before Closed; complete during closing animation and require no late notice/restoration/Hide; success-first closes once. Verify busy face-flyout callback and retry keyboard focus. Real session writes require separate authorization/test-owned content; none performed here.

Biggest risks: default close escaping before first await, reentrant callbacks during success/close, stale finally restoring closed UI, treating malformed code as success, editing draft during pending post, and duplicate manual resend after unknown delivery. The contract above addresses each without shared transport/parser/credential changes.

**Disposition:** requirements prepared with actual-source failing evidence; implementation NOT STARTED. Execute only after independent PASS and U1/PB1/source-test ownership release. Complete offline slice honestly; retain explicit Windows/session blockers and existing accepted-device policy. This artifact does not reopen shipped features or declare whole-repository stop conditions satisfied.
