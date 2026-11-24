# E1 / UX-02 execution

Status: **IMPLEMENTED + OFFLINE VERIFIED; independent implementation review pending; Windows XAML/build/routing/device/session gates BLOCKED.**

U9 source/test commit `7ff548b24dc70a335c6d0d44c4ebc86a984ce96c` pushed/remote-verified first. E1 startup `git pull --ff-only` returned Already up to date. Foundation immutable commits and other owners' in-flight cloud/search/build work are preserved. Exact staged ownership excludes all their files.

## Real-source red and unchanged green

Added permanent linked real-dialog tests and actual response extraction **before modifying either dialog source file**. SDK 8.0.427/runtime 8.0.33 and pinned Newtonsoft.Json 13.0.3, from repository root:

```bash
export PATH=/workspace/work/maintenance-foundation/quality/dotnet:$PATH
export UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality
export DOTNET_ROOT=$UPSTREAM_UNIT_TOOLS/dotnet
export DOTNET_CLI_HOME=$UPSTREAM_UNIT_TOOLS/cache/dotnet-cli
export NUGET_PACKAGES=$UPSTREAM_UNIT_TOOLS/cache/nuget
bash plan/tests/comments/run.sh
git diff --check
```

Pre-correction real source at U9 HEAD: comments compiled/executed, original parser/U1/PB1 checks passed, then **153 dialog checks / 109 FAILURES; exit 1**. Failures include uncancelled primary/blank, malformed-code/failed-transport success, rejection retry, absent busy/read-only/task owner, duplicate send/face mutation, secondary/Closing/Closed fallback late publication, fresh-B isolation and escaped async exceptions. Old dialog SHA-256 `4cc635221899d3a8e537af55306ed9f8ee15c11d723ea85dab5fa2a124c83871`. Raw log `/workspace/work/maintenance-foundation/api-action-execution/e1-red.log`; this is genuine runtime behavior after successful compilation, not a stub compiler error.

After the scoped product correction, **the same permanent behavioral assertions unchanged** pass: **DIALOG_CHECKS=153 FAILURES=0; PASS 1070 assertions; exit 0**. The prior 917 parser/mapper/request/PB1 assertions remain and execute, with actual response decoding replacing the formerly strict-only double as explicitly reviewed. Last run after runner portability/XAML spacing/comment refinements has the same result. Full green output: scratch `e1-green.log`.

## Product behavior / ownership

Only `Dialogs/SendCommentDialog.xaml.cs` and `.xaml` change. Primary is synchronous and sets Cancel before validation/dispatch; one held exception-contained Task captures exact text/target/type, guards one in-flight request before awaiting and rejects callbacks after terminal retirement. Sending locks editing and both face opener/already-open item callbacks, disables send and shows indeterminate progress/`正在发送`. Blank input retains editable draft and inline validation. Existing typed action parser owns acceptance with optional data; rejection/invalid/transport/fault return editable focused retry while preserving draft; uncertain delivery explicitly advises refresh before retry. No automatic retransmission.

Accepted success retires before one notice/Hide. Secondary invalidates synchronously before its single explicit Hide and cancels default handling. Constructor wires synchronous earliest Closing retirement plus idempotent Closed fallback; late continuations cannot restore/send/notice/Hide during the Closing→Closed interval. Logging uses safe category-only text, without raw draft/body/response/exception payload. Emote async event faults are contained. UI status uses existing theme brush, shared spacing resource and a polite live region. No helper/parser/transport/auth/API-builder changes occur in this slice.

## Test seams and extraction fidelity

- Existing `comments/CommentsProbe.csproj` links whole real dialog, real legacy `Models/ApiModel.cs` (to compile complete actual response class), and generated actual HttpResults, preserving all prior real builder/parser/DTO inputs.
- `run.sh` uses Bash/awk to copy from the exact `public class HttpResults` boundary through the existing namespace end into ignored `obj/probe-generated/HttpResults.cs`. Only required usings and namespace opener are prepended; **no method/class-body rewriting**. Missing extraction boundary fails the runner. The boundary regex permits CRLF while retaining source class bytes. No Python/runtime dependency or generated copy is committed.
- `CommentProbeStubs.cs` removes its strict-only HttpResults stand-in and adds controlled Task delivery/capture and application-service recorders. `DialogPlatformStubs.cs` supplies generated-field/platform/emote primitives only; Hide merely records, and Closing/Closed are independently raised without lifecycle decisions.
- `DialogRecoveryChecks.cs` drives real private handlers using reflection, deterministic TaskCompletionSource delivery and a synchronization-context queue, with no sleeps/live network. It records escaped async-void exceptions and verifies explicit held Task ownership/completion. No send/recovery/decoder decision is duplicated in a stub.

153 checks include null/empty/whitespace/linebreak-only input, exact Unicode/multiline/space/opaque-ID/type snapshots, deferred rejection→explicit retry, duplicate callbacks, face callback protection, synchronous/delayed faults, code types/bounds, all 15 specified raw-body compatibility fixtures through **actual dialog + parser + decoding**, optional success payloads and null response. Separate secondary/Closing/Closed-fallback × success/rejection/fault races complete before Closed; repeated Closed and success-first ordering are checked, as is A's pending close interval against B's draft/status/busy state/global notices.

Source fidelity: an independent byte-suffix comparison confirmed the entire original HttpResults class and its closing namespace bytes are the exact generated suffix (`HTTPRESULTS_CLASS_AND_NAMESPACE_SUFFIX=EXACT`). SHA-256:

| Input | SHA-256 |
| --- | --- |
| unchanged product HttpHelper.cs | `b12e457a75a133e9298fb77ad982b33986c2f95da2562b4f031b1fd692424d13` |
| generated HttpResults wrapper, red and green | `8aa4c8734aed85517df1dc3863a479fc076a262e775a7c657e45e9f8b9bc6a93` |
| corrected whole dialog code-behind | `71cc068c7e38fc70eb12d9d8e4cc087b4f43a5fc2eafe47a451bd102524ccde6` |

The decoder retains longest-object salvage and first-on-equal-length semantics. Embedded/prefixed zero can succeed only for current successful transport, as required; plain unparseable HTML, malformed/incomplete fixtures, selected-object missing/noninteger/out-of-range code fail. This does not claim universal HTML rejection, a general syntax-aware salvage extractor or strict document semantics. Legacy real GetJson generates its existing CS1998 warning in red/green; no warning is suppressed. Stub events/properties establish state decisions only, not actual WinRT event routing/rendering or server delivery.

## Remaining gates and commit

`git diff --check` passed. XML parsing of edited XAML passed as **well formed XML only**, not UWP compilation. The required Windows command remains blocked: U9 attempt `powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump -Restore` exited **127 / powershell: command not found** on this same Linux host. No Windows/VS/UWP SDK, installed app, actual Closing/Escape/animation/focus/keyboard/Narrator/face rendering or authenticated write verification exists. No release-ready/device/session success claim. No real write was performed.

Windows follow-up: delayed send remains visible/busy/cancellable; rejection preserves draft and keyboard retry; blank remains open; verify Escape/external Hide Closing precedes Closed and completion between them cannot publish/restore/Hide; success-first closes once; already-open face callback cannot alter protected draft; status/live region/theme is readable. Real writes require separate authorization/test-owned content.

Source/test commit is `fix: preserve comment drafts through send failures`. Its stable SHA and exact remote confirmation are appended after push. Shared/main documentation remains with its later owner, and source test ownership is released only after the execution-index artifact is finalized. No whole-suite result follows from this affected batch.
