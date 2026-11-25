# SeasonReviewVM single-flight ownership requirements

Status: READY FOR INDEPENDENT REVIEW. Scope frozen 2026-11-25; only a reviewed executor may implement. The requirements author has released requirements/evidence ownership after the hashes in the private release receipt.

## Evidence and problem

`SeasonDetailPage.xaml.cs:689-695` selects reviews at pivot index 3 and directly awaits `seasonReviewVM.GetItems()` while Items is null/empty. The first held response leaves Items empty, so leaving/reentering calls GetItems again. Its public entry has no Loading check; only Refresh/LoadMore do. Real full current VM/API, real IModules notifications, and the exact extracted production event method reproduce two dispatches and two appended copies before any native Windows claim. See requirements-evidence.md and its private bound proof.

The `MediaID == 0` return currently occurs inside try, so its finally sets Loading=false even when this invocation never acquired the active request. With another request held, that releases the command guard and permits a duplicate LoadMore. Loading notifications are synchronous (`IModules.DoPropertyChanged` invokes PropertyChanged directly); a true-notification handler can reenter the public method before transport dispatch and currently creates a second request.

## Required behavior

1. A SeasonReviewVM instance admits at most one GetItems read operation at a time on its existing UI-thread/reentrant call path. Claim ownership before any observable property notification or request dispatch. Reject reentry at the public method itself; commands and the real pivot receive the same protection.
2. A rejected invocation must return without dispatch, append/reset, cursor mutation, CanLoadMore mutation, Loading notification, or owner release. In particular zero-ID rejection must happen outside the owner's cleanup region and must not alter an already active owner's Loading. A zero-ID idle call stays a no-op.
3. Only the accepted operation performs its existing try/catch/finally cleanup. Hold its claim through synchronous notifications during completion, then release it exactly once. Reentry from Loading=true, CanLoadMore changes, or Loading=false notifications must not start a second owner or release another one. After completion/error fully returns, a later normal call must be accepted.
4. Preserve the existing single-call behavior: set Loading=true / CanLoadMore=false before request; append decoded review rows; retain current Next/total terminal rules; transport/business/parse/exception handling and toast behavior remain as today; accepted failure releases ownership and permits retry. Refresh still clears rows and resets Next when idle; busy Refresh/LoadMore remain no-ops. No new queuing, automatic retry, cancellation, generation replacement, or cursor policy.
5. Preserve public method/command/API signatures and API route/query/auth behavior. This is an in-instance reentrancy guard, not a cross-thread or process synchronization redesign. No claim of native event delivery, device UX, decoded video memory, Windows build, or hosted CI follows from these managed probes.

## Smallest implementation and test ownership proposal

Product: **only** `src/BiliLite.UWP/Modules/Season/SeasonReviewVM.cs`, limited to GetItems ownership state/guard/cleanup. A private owner flag is appropriate because public Loading is observable and its completion notification can synchronously reenter. Do not change page/API/parsing/other VMs or review write methods.

Extend existing season1 batch (no new batch or aggregate inventory change):

- `plan/tests/season1/Season1Probe.csproj`: link full real SeasonReviewVM.
- `plan/tests/season1/Season1Stubs.cs`: minimal compile/transport seams required for that VM; review write methods must remain unexecuted/throwing boundaries. Keep existing season tests' behavior.
- `plan/tests/season1/SeasonReviewOwnershipProbe.cs`: new held-task/reentry/error controls using the real VM. Clearly label API and native boundaries.
- `plan/tests/season1/extract-lifetime.py`: extract the exact current review pivot event method into an isolated caller harness using real SeasonReviewVM; retain source hash/anchor binding and existing slices. The existing lifetime fake SeasonReviewVM is a distinct namespace and should remain unchanged.
- `plan/tests/season1/Program.cs`: invoke/await these checks from the existing executable.

Existing api02 short-review request checks remain the real API builder control. The private full real API/VM fixture is an additional validation/discovery input, not product code. If the existing season1 compile seam is unexpectedly larger than these files, stop and record the blocker rather than expanding to a new batch, shared stub, or runner configuration without a revised grant.

## Acceptance matrix

- **Held pivot path:** exact extracted caller index 3; first request held, leave index 0/reenter 3 repeatedly; exactly one dispatch, Loading=true, empty rows and stable cursor until release. Complete once: one row appended once, normal cursor/content/CanLoadMore behavior, Loading=false. Reentry calls themselves must finish without releasing owner.
- **Public/commands:** while first read is held call public GetItems, RefreshCommand, LoadMoreCommand; assert no dispatch, no clear/reset, no false Loading notification, no state change. After owner completion, one new call may dispatch normally.
- **Zero ID:** idle zero-ID call does no dispatch or notification and preserves seeded rows/Next/CanLoadMore; during active held read set MediaID=0 and call GetItems, assert original remains Loading=true and held. Restore ID; commands/public calls remain blocked until owner finishes. Owner's existing response semantics are preserved; media-generation redesign is out of scope.
- **Synchronous reentry:** one-shot PropertyChanged observers call GetItems during Loading=true, CanLoadMore=false, and Loading=false notifications; each nested call causes no request or state release. After all notification handlers and original operation complete, retry succeeds. Use a one-shot callback and bounded completion waits so a regression fails instead of hanging.
- **Errors:** held request resolved to transport failure, business rejection, malformed/parse exception, and faulted/throwing request; each admitted owner releases Loading/claim, keeps existing rows and Next, preserves current CanLoadMore/toast behavior, and allows a fresh retry. Test through original full VM, not a copied guard implementation.
- **Successful control:** ordinary one-page/append/terminal, HTML entity decoding, opaque Next reuse, and idle refresh reset remain as current behavior. Existing `api02` request matrix remains green (URL escaping, GET, media/ps/sort, optional access key, configured host).
- **Regression sensitivity:** baseline current source must fail desired duplicate/zero-ID/reentry assertions; fixed source must pass. Record source hashes and actual command outputs. Do not weaken prior assertions.

## Verification / limits / risk

Run isolated development checks then targeted `bash plan/tests/run-all.sh --tools-root /workspace/work/maintenance-foundation/quality --only batch:season1 --only batch:api02`; selected success is PARTIAL_PASS. Follow with the normal complete aggregate gate if the remaining fixed time permits; evidence must explicitly distinguish a selected gate from full 51 logical/52 executable stages. Use the existing exact SDK 8.0.427/runtime 8.0.33. No Windows/native/build/device result is supplied by this requirements phase. Final executor grant may include separately owned release documentation; this file grants no edits itself.

Risk: moving owner admission after an observable setter permits synchronous recursion; releasing before the Loading=false notification permits a nested owner during cleanup. The change must keep property notifications within the accepted owner's claim. Avoid changing general parser/paging error behavior merely to simplify testing.
